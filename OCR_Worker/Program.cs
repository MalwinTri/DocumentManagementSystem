using System.Diagnostics;
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

// --- Config aus ENV ---
var rabbitHost   = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? "rabbitmq";
var rabbitQueue  = Environment.GetEnvironmentVariable("RABBIT_QUEUE") ?? "ocr-queue";
var prefetchStr  = Environment.GetEnvironmentVariable("RABBIT_PREFETCH") ?? "1";
var enableDlq    = (Environment.GetEnvironmentVariable("RABBIT_ENABLE_DLQ") ?? "true")
                    .Equals("true", StringComparison.OrdinalIgnoreCase);
var storageRoot  = Environment.GetEnvironmentVariable("STORAGE_ROOT") ?? "/app";   // z.B. /app
var storageSub   = Environment.GetEnvironmentVariable("STORAGE_DOCS_SUBFOLDER") ?? "files"; // z.B. files

ushort prefetch = ushort.TryParse(prefetchStr, out var p) ? p : (ushort)1;

// --- RabbitMQ 6.x: sync API ---
var factory = new ConnectionFactory
{
    HostName = rabbitHost,
    DispatchConsumersAsync = true // wichtig für AsyncEventingBasicConsumer
};
using var conn = factory.CreateConnection();   // IConnection
using var ch   = conn.CreateModel();           // IModel

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

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var consumer    = new AsyncEventingBasicConsumer(ch);

// 6.x: Event heißt "Received"
consumer.Received += async (_, ea) =>
{
    try
    {
        var message = Encoding.UTF8.GetString(ea.Body.ToArray());

        OcrJobMessage? msg = null;
        try { msg = JsonSerializer.Deserialize<OcrJobMessage>(message, jsonOptions); } catch { /* ignore */ }

        if (msg is null)
        {
            logger.LogWarning("Invalid OCR message: {Payload}", message);
            ch.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        // Fallback-Pfad, falls LocalPath nicht mitkommt
        var pdfPath = !string.IsNullOrWhiteSpace(msg.LocalPath)
            ? msg.LocalPath!
            : Path.Combine(storageRoot, storageSub, $"{msg.DocumentId}.pdf");

        logger.LogInformation("Received OCR job: DocumentId={Id}, pdfPath={Path}", msg.DocumentId, pdfPath);

        if (!File.Exists(pdfPath))
            throw new FileNotFoundException($"PDF not found at {pdfPath}");

        // OCR ausführen
        var text = await OcrPdfAsync(pdfPath);
        var txtPath = Path.ChangeExtension(pdfPath, ".txt")!;
        await File.WriteAllTextAsync(txtPath, text, Encoding.UTF8);

        logger.LogInformation("OCR done for {Id}. Wrote: {TxtPath} (len={Len})",
            msg.DocumentId, txtPath, text.Length);

        ch.BasicAck(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing message");
        ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: !enableDlq);
    }
};

ch.BasicConsume(queue: rabbitQueue, autoAck: false, consumer: consumer);
logger.LogInformation("OCR Worker (RabbitMQ 6.6) started. Queue={Queue} Host={Host} Prefetch={Prefetch} DLQ={DLQ} Root={Root}/{Sub}",
    rabbitQueue, rabbitHost, prefetch, enableDlq, storageRoot, storageSub);

// graceful shutdown
var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; tcs.TrySetResult(); };
AppDomain.CurrentDomain.ProcessExit += (_, __) => tcs.TrySetResult();
await tcs.Task;

// --------- Helpers ----------

static async Task<string> OcrPdfAsync(string pdfPath)
{
    // Workdir
    var work = Directory.CreateTempSubdirectory("ocr");
    var tiffPattern = Path.Combine(work.FullName, "page-%03d.tiff");
    try
    {
        // 1) Ghostscript: PDF -> TIFF
        await RunCli("gs", $"-q -dNOPAUSE -dBATCH -sDEVICE=tiffgray -r300 -sOutputFile={tiffPattern} \"{pdfPath}\"");

        // 2) Tesseract: jede Seite -> Text
        var sb = new StringBuilder();
        foreach (var tiff in Directory.GetFiles(work.FullName, "page-*.tiff").OrderBy(x => x))
        {
            var pageText = await RunCli("tesseract", $"\"{tiff}\" stdout -l deu+eng --dpi 300");
            sb.AppendLine(pageText);
        }
        return sb.ToString().Trim();
    }
    finally
    {
        try { Directory.Delete(work.FullName, true); } catch { /* ignore */ }
    }
}

static async Task<string> RunCli(string file, string args)
{
    var psi = new ProcessStartInfo(file, args)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    using var p = Process.Start(psi)!;
    var stdout = await p.StandardOutput.ReadToEndAsync();
    var stderr = await p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();
    if (p.ExitCode != 0) throw new InvalidOperationException($"{file} failed: {stderr}");
    return stdout;
}

// Message-Contract (LocalPath optional)
public sealed class OcrJobMessage
{
    public Guid DocumentId { get; init; }
    public string? LocalPath { get; init; }  // <— optional, Fallback baut /app/files/<id>.pdf
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public DateTime UploadedAt { get; init; }
}
