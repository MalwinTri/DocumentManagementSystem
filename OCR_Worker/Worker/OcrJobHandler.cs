using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.OCR_Worker.OCR;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.OCR_Worker.Worker;

public class OcrJobHandler
{
    private readonly IOcrEngine _ocr;
    private readonly ILogger<OcrJobHandler> _log;

    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public OcrJobHandler(IOcrEngine ocr, ILogger<OcrJobHandler> log)
    {
        _ocr = ocr;
        _log = log;

        var endpoint = Required("GARAGE_S3_ENDPOINT").TrimEnd('/');
        var region = Env("GARAGE_S3_REGION") ?? "garage";
        _bucket = Required("GARAGE_S3_BUCKET");
        var accessKey = Required("GARAGE_S3_ACCESS_KEY");
        var secretKey = Required("GARAGE_S3_SECRET_KEY");

        var cfg = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = region
        };
        _s3 = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), cfg);
    }

    public async Task HandleAsync(OcrJob job, CancellationToken ct)
    {
        var pdfKey = $"{job.DocumentId}.pdf";
        var txtKey = $"{job.DocumentId}.txt";

        _log.LogInformation("Download from Garage: s3://{Bucket}/{Key}", _bucket, pdfKey);

        using var get = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucket,
            Key = pdfKey
        }, ct);

        await using var pdfMem = new MemoryStream();
        await get.ResponseStream.CopyToAsync(pdfMem, ct);
        pdfMem.Position = 0;

        // OCR
        var text = await _ocr.ExtractTextAsync(pdfMem, ct);

        // Upload .txt zurück nach Garage (mit ContentLength => kein chunked)
        var bytes = Encoding.UTF8.GetBytes(text);
        await using var txtStream = new MemoryStream(bytes);

        var put = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = txtKey,
            InputStream = txtStream,
            ContentType = "text/plain; charset=utf-8"
        };
        put.Headers.ContentLength = txtStream.Length;

        await _s3.PutObjectAsync(put, ct);

        _log.LogInformation("Uploaded OCR result: s3://{Bucket}/{Key} (len={Len})",
            _bucket, txtKey, bytes.Length);
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Missing environment variable: {name}");

    private static string? Env(string name) => Environment.GetEnvironmentVariable(name);
}
