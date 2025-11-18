using Amazon.Runtime;
using Amazon.S3;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.OCR_Worker.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------
var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
}));
var logger = loggerFactory.CreateLogger("OcrWorker");

// ------------------------------------------------------------
// Konfiguration
// ------------------------------------------------------------
var config = new ConfigurationBuilder()
    .AddJsonFile("/config/appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

static string? Env(string name) => Environment.GetEnvironmentVariable(name);

static string GetEnv(string name, string? def = null, bool required = false)
{
    var v = Env(name) ?? def;
    if (required && string.IsNullOrWhiteSpace(v))
        throw new ArgumentNullException(name);
    return v!;
}

string GetS3(string env, string jsonKey, bool required = true)
{
    var v = Env(env) ?? config[jsonKey];
    if (required && string.IsNullOrWhiteSpace(v))
        throw new ArgumentNullException(env);
    return v!;
}

// ------------------------------------------------------------
// DB-ConnectionString
// ------------------------------------------------------------
var dbConnectionString =
    config.GetConnectionString("Default")
    ?? Env("DB_CONNECTION")
    ?? throw new InvalidOperationException("Keine ConnectionStrings:Default oder DB_CONNECTION gefunden");

// ------------------------------------------------------------
// RabbitMQ
// ------------------------------------------------------------
var rabbitHost = GetEnv("RABBIT_HOST", config["Rabbit:Host"] ?? "rabbitmq");
var rabbitQueue = GetEnv("RABBIT_QUEUE", config["Rabbit:Queue"] ?? "ocr-queue");
var rabbitUser = GetEnv("RABBIT_USER", config["Rabbit:User"] ?? "guest");
var rabbitPass = GetEnv("RABBIT_PASS", config["Rabbit:Pass"] ?? "guest");
var prefetchStr = GetEnv("RABBIT_PREFETCH", config["Rabbit:Prefetch"] ?? "1");
var enableDlq = (GetEnv("RABBIT_ENABLE_DLQ", config["Rabbit:RequeueOnError"]) ?? "true")
    .Equals("true", StringComparison.OrdinalIgnoreCase);
ushort prefetch = ushort.TryParse(prefetchStr, out var p) ? p : (ushort)1;

// ------------------------------------------------------------
// Garage S3 
// ------------------------------------------------------------
var s3Endpoint = GetS3("GARAGE_S3_ENDPOINT", "GarageS3:Endpoint");
var s3Region = GetS3("GARAGE_S3_REGION", "GarageS3:Region");
var s3Bucket = GetS3("GARAGE_S3_BUCKET", "GarageS3:Bucket");
var s3AccessKey = GetS3("GARAGE_S3_ACCESS_KEY", "GarageS3:AccessKey");
var s3SecretKey = GetS3("GARAGE_S3_SECRET_KEY", "GarageS3:SecretKey");

// ------------------------------------------------------------
// OCR-Optionen
// ------------------------------------------------------------
var ocrLang = config["Ocr:Language"] ?? Env("OCR_LANG") ?? "eng+deu";
var tessdataPrefix = config["Ocr:TessdataPrefix"] ?? Env("TESSDATA_PREFIX"); // optional
var ocrDpiStr = config["Ocr:Dpi"] ?? Env("OCR_DPI") ?? "300";
var ocrDpi = int.TryParse(ocrDpiStr, out var dpi) ? dpi : 300;

// ------------------------------------------------------------
// S3-Client
// ------------------------------------------------------------
var credentials = new BasicAWSCredentials(s3AccessKey, s3SecretKey);
var s3Config = new AmazonS3Config
{
    ServiceURL = s3Endpoint.TrimEnd('/'),
    ForcePathStyle = true,
    UseHttp = s3Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
    AuthenticationRegion = s3Region
};
IAmazonS3 s3 = new AmazonS3Client(credentials, s3Config);

// ------------------------------------------------------------
// Handler erzeugen
// ------------------------------------------------------------
var handler = new OcrJobHandler(
    loggerFactory.CreateLogger<OcrJobHandler>(),
    s3,
    s3Bucket,
    dbConnectionString,
    ocrLang,
    ocrDpi,
    tessdataPrefix
);

// ------------------------------------------------------------
// RabbitMQ-Verbindung
// ------------------------------------------------------------
var factory = new ConnectionFactory
{
    HostName = rabbitHost,
    UserName = rabbitUser,
    Password = rabbitPass,
    DispatchConsumersAsync = true
};
using var conn = factory.CreateConnection();
using var ch = conn.CreateModel();

IDictionary<string, object>? queueArgs = null;
if (enableDlq)
{
    const string dlxName = "ocr-dlx";
    const string dlqName = "ocr-dead";
    const string dlqRoute = "ocr-dead";

    ch.ExchangeDeclare(dlxName, ExchangeType.Direct, durable: true);
    ch.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
    ch.QueueBind(dlqName, dlxName, routingKey: dlqRoute);

    queueArgs = new Dictionary<string, object>
    {
        ["x-dead-letter-exchange"] = dlxName,
        ["x-dead-letter-routing-key"] = dlqRoute
    };
}

ch.QueueDeclare(queue: rabbitQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
ch.BasicQos(0, prefetchCount: prefetch, global: false);

logger.LogInformation(
    "OCR Worker starting. Rabbit={Host} Queue={Queue} Prefetch={Prefetch} DLQ={DLQ} S3={S3} Bucket={Bucket} Lang={Lang} DPI={Dpi}",
    rabbitHost, rabbitQueue, prefetch, enableDlq, s3Endpoint, s3Bucket, ocrLang, ocrDpi);

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var consumer = new AsyncEventingBasicConsumer(ch);

// ------------------------------------------------------------
// Message-Handling
// ------------------------------------------------------------
consumer.Received += async (_, ea) =>
{
    try
    {
        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
        OcrJob? msg = null;
        try
        {
            msg = JsonSerializer.Deserialize<OcrJob>(message, jsonOptions);
        }
        catch
        {
            // ignorieren, wird unten als invalid behandelt
        }

        if (msg is null || msg.DocumentId == Guid.Empty)
        {
            logger.LogWarning("Invalid OCR message: {Payload}", message);
            ch.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        // hier nur noch an den Handler delegieren
        await handler.HandleAsync(msg, CancellationToken.None);

        ch.BasicAck(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing OCR message");
        ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: !enableDlq);
    }
};

ch.BasicConsume(queue: rabbitQueue, autoAck: false, consumer: consumer);
logger.LogInformation("OCR Worker ready.");

// Shutdown
var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    tcs.TrySetResult();
};
AppDomain.CurrentDomain.ProcessExit += (_, __) => tcs.TrySetResult();
await tcs.Task;
