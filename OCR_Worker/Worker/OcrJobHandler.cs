using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.OCR_Worker.OCR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.OCR_Worker.Worker;

public sealed class OcrJobHandler
{
    private readonly IOcrEngine _ocr;
    private readonly ILogger<OcrJobHandler> _log;
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public OcrJobHandler(
        IOcrEngine ocr,
        ILogger<OcrJobHandler> log,
        IOptions<GarageS3Options> s3opts)
    {
        _ocr = ocr;
        _log = log;

        var o = s3opts.Value ?? throw new InvalidOperationException("GarageS3 options missing");

        _bucket = string.IsNullOrWhiteSpace(o.Bucket) ? "documents" : o.Bucket;

        var cfg = new AmazonS3Config
        {
            ServiceURL = (o.Endpoint ?? throw new InvalidOperationException("GarageS3:Endpoint missing")).TrimEnd('/'),
            AuthenticationRegion = string.IsNullOrWhiteSpace(o.Region) ? "garage" : o.Region,
            ForcePathStyle = true,
            UseHttp = o.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        };

        if (string.IsNullOrWhiteSpace(o.AccessKey) || string.IsNullOrWhiteSpace(o.SecretKey))
            throw new InvalidOperationException("GarageS3:AccessKey/SecretKey missing");

        _s3 = new AmazonS3Client(new BasicAWSCredentials(o.AccessKey, o.SecretKey), cfg);
    }

    public async Task HandleAsync(OcrJob job, CancellationToken ct)
    {
        // Prefer S3Key from message; fallback to {DocumentId}.pdf
        var pdfKey = !string.IsNullOrWhiteSpace(job.S3Key) ? job.S3Key : $"{job.DocumentId}.pdf";
        var txtKey = System.IO.Path.ChangeExtension(pdfKey, ".txt")!;

        try
        {
            _log.LogInformation("Downloading: s3://{Bucket}/{Key}", _bucket, pdfKey);

            using var get = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucket,
                Key = pdfKey
            }, ct);

            // Direkt mit ResponseStream -> OCR
            var text = await _ocr.ExtractTextAsync(get.ResponseStream, ct);

            // Upload .txt (fixe Länge, kein Chunked)
            var bytes = Encoding.UTF8.GetBytes(text);
            await using var txtStream = new MemoryStream(bytes);

            var put = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = txtKey,
                InputStream = txtStream,
                ContentType = "text/plain; charset=utf-8",
                UseChunkEncoding = false
            };
            put.Headers.ContentLength = txtStream.Length;

            await _s3.PutObjectAsync(put, ct);

            _log.LogInformation("Uploaded OCR result: s3://{Bucket}/{Key} (len={Len})",
                _bucket, txtKey, bytes.Length);
        }
        catch (AmazonS3Exception s3ex) when (s3ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _log.LogWarning("PDF not found: s3://{Bucket}/{Key}", _bucket, pdfKey);
            throw;
        }
    }
}

public sealed class GarageS3Options
{
    public string Endpoint { get; set; } = "";
    public string Region { get; set; } = "garage";
    public string Bucket { get; set; } = "documents";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
}
