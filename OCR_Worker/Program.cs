using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
}));
var logger = loggerFactory.CreateLogger("OcrWorker");

var rabbitHost = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? "rabbitmq";
var rabbitQueue = Environment.GetEnvironmentVariable("RABBIT_QUEUE") ?? "ocr-queue";
var prefetchStr = Environment.GetEnvironmentVariable("RABBIT_PREFETCH") ?? "1";
var enableDlq = (Environment.GetEnvironmentVariable("RABBIT_ENABLE_DLQ") ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);
ushort prefetch = ushort.TryParse(prefetchStr, out var p) ? p : (ushort)1;

var factory = new ConnectionFactory { HostName = rabbitHost };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

IDictionary<string, object?>? queueArgs = null;
if (enableDlq)
{
    const string dlxName = "ocr-dlx";
    const string dlqName = "ocr-dead";
    const string dlqRoute = "ocr-dead";

    await channel.ExchangeDeclareAsync(dlxName, ExchangeType.Direct, durable: true);
    await channel.QueueDeclareAsync(dlqName, durable: true, exclusive: false, autoDelete: false);
    await channel.QueueBindAsync(dlqName, dlxName, routingKey: dlqRoute);

    queueArgs = new Dictionary<string, object?>
    {
        ["x-dead-letter-exchange"] = dlxName,
        ["x-dead-letter-routing-key"] = dlqRoute
    };
}

await channel.QueueDeclareAsync(
    queue: rabbitQueue,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: queueArgs
);

await channel.BasicQosAsync(0, prefetchCount: prefetch, global: false);

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        try
        {
            var doc = JsonSerializer.Deserialize<OcrJobMessage>(message, jsonOptions); 
            if (doc is not null)
            {
                logger.LogInformation(
                    "Received OCR job: DocumentId={DocumentId}, FileName={FileName}, UploadedAt={UploadedAt}",
                    doc.DocumentId, doc.FileName, doc.UploadedAt);
            }
            else
            {
                logger.LogInformation("Received OCR message (raw): {Message}", message);
            }
        }
        catch
        {
            logger.LogInformation("Received OCR message (raw): {Message}", message);
        }

        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing message");
        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: !enableDlq);
    }
};

await channel.BasicConsumeAsync(queue: rabbitQueue, autoAck: false, consumer: consumer);
logger.LogInformation("OCR Worker (7.x) started. Waiting on {Queue} @ {Host} ... Prefetch={Prefetch}, DLQ={DLQ}",
    rabbitQueue, rabbitHost, prefetch, enableDlq);

var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; tcs.TrySetResult(); };
AppDomain.CurrentDomain.ProcessExit += (_, __) => tcs.TrySetResult();
await tcs.Task;

public sealed class OcrJobMessage
{
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = "";
    public string? ContentType { get; init; }
    public DateTime UploadedAt { get; init; }
}
