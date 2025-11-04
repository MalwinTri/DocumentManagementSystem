using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------
var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
}));
var logger = loggerFactory.CreateLogger("OcrWorker");


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
// RabbitMQ 6.x
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

logger.LogInformation("OCR Worker starting. Rabbit={Host} Queue={Queue} Prefetch={Prefetch} DLQ={DLQ} S3={S3} Bucket={Bucket} Lang={Lang} DPI={Dpi}",
    rabbitHost, rabbitQueue, prefetch, enableDlq, s3Endpoint, s3Bucket, ocrLang, ocrDpi);

await LogCliVersion("gs", "-v", "Ghostscript", logger);
await LogCliVersion("tesseract", "--version", "Tesseract", logger);

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var consumer = new AsyncEventingBasicConsumer(ch);

consumer.Received += async (_, ea) =>
{
    try
    {
        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
        OcrJob? msg = null;
        try { msg = JsonSerializer.Deserialize<OcrJob>(message, jsonOptions); } catch { /* ignore */ }

        if (msg is null || msg.DocumentId == Guid.Empty || string.IsNullOrWhiteSpace(msg.S3Key))
        {
            logger.LogWarning("Invalid OCR message: {Payload}", message);
            ch.BasicAck(ea.DeliveryTag, multiple: false);
            return;
        }

        logger.LogInformation("Received OCR job: DocumentId={Id}, S3Key={Key}", msg.DocumentId, msg.S3Key);

        // 1) PDF aus Garage S3 holen
        await using var pdfStream = await DownloadPdfAsync(s3, s3Bucket, msg.S3Key);

        // 2) OCR -> Text
        var text = await OcrPdfStreamAsync(pdfStream, ocrLang, ocrDpi, tessdataPrefix, logger);

        // 3) Text zurück ins S3 (gleicher Bucket, gleiche Id, .txt)
        var txtKey = System.IO.Path.ChangeExtension(msg.S3Key, ".txt")!;
        await UploadTextAsync(s3, s3Bucket, txtKey, text);

        logger.LogInformation("OCR done for {Id}. Uploaded: s3://{Bucket}/{Key} (len={Len})",
            msg.DocumentId, s3Bucket, txtKey, text.Length);

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

// shutdown
var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; tcs.TrySetResult(); };
AppDomain.CurrentDomain.ProcessExit += (_, __) => tcs.TrySetResult();
await tcs.Task;

// ======================= Helpers =======================

static async Task<Stream> DownloadPdfAsync(IAmazonS3 s3, string bucket, string key)
{
    using var resp = await s3.GetObjectAsync(new GetObjectRequest
    {
        BucketName = bucket,
        Key = key
    });
    var ms = new MemoryStream();
    await resp.ResponseStream.CopyToAsync(ms);
    ms.Position = 0;
    return ms;
}

static async Task UploadTextAsync(IAmazonS3 s3, string bucket, string key, string text)
{
    var bytes = Encoding.UTF8.GetBytes(text);
    await using var ms = new MemoryStream(bytes);

    var put = new PutObjectRequest
    {
        BucketName = bucket,
        Key = key,
        InputStream = ms,
        ContentType = "text/plain; charset=utf-8",
        UseChunkEncoding = false
    };
    put.Headers.ContentLength = ms.Length;

    await s3.PutObjectAsync(put);
}

static async Task<string> OcrPdfStreamAsync(Stream pdfStream, string lang, int dpi, string? tessdataPrefix, ILogger logger)
{
    var work = Directory.CreateTempSubdirectory("ocr");
    var pdfPath = Path.Combine(work.FullName, "input.pdf");
    await using (var fs = File.Create(pdfPath)) { pdfStream.Position = 0; await pdfStream.CopyToAsync(fs); }

    var tiffPattern = Path.Combine(work.FullName, "page-%03d.tiff");
    try
    {
        // PDF -> TIFF (grau, 300dpi) via Ghostscript
        await RunCli("gs", $"-q -dNOPAUSE -dBATCH -sDEVICE=tiffgray -r{dpi} -sOutputFile=\"{tiffPattern}\" \"{pdfPath}\"");

        var sb = new StringBuilder();
        foreach (var tiff in Directory.GetFiles(work.FullName, "page-*.tiff").OrderBy(x => x))
        {
            // Tesseract: ggf. TESSDATA_PREFIX setzen
            var env = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(tessdataPrefix))
                env["TESSDATA_PREFIX"] = tessdataPrefix;

            var pageText = await RunCli("tesseract", $"\"{tiff}\" stdout -l {lang} --dpi {dpi}", env);
            sb.AppendLine(pageText);
        }
        return sb.ToString().Trim();
    }
    finally
    {
        try { Directory.Delete(work.FullName, true); } catch { /* ignore */ }
    }
}

static async Task<string> RunCli(string file, string args, IDictionary<string, string>? extraEnv = null)
{
    var psi = new ProcessStartInfo(file, args)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    if (extraEnv != null)
        foreach (var kv in extraEnv) psi.Environment[kv.Key] = kv.Value;

    using var p = Process.Start(psi)!;
    var stdout = await p.StandardOutput.ReadToEndAsync();
    var stderr = await p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();
    if (p.ExitCode != 0) throw new InvalidOperationException($"{file} failed: {stderr}");
    return stdout;
}

static async Task LogCliVersion(string exe, string arg, string label, ILogger logger)
{
    try
    {
        var output = await RunCli(exe, arg);
        var first = output.Split('\n').FirstOrDefault()?.Trim();
        logger.LogInformation("{Label}: {Line}", label, first);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "{Label} not available or failed to run", label);
    }
}

// Nachricht (API sendet DocumentId + S3Key)
public sealed class OcrJob
{
    public Guid DocumentId { get; init; }
    public string S3Key { get; init; } = default!;
    public string? ContentType { get; init; }
    public DateTime UploadedAt { get; init; }
}
