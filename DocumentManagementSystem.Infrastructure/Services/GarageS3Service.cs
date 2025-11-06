using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace DocumentManagementSystem.Infrastructure.Services
{
    public class GarageS3Service : IGarageS3Service
    {
        private readonly IAmazonS3 _client;
        private readonly string _bucket;

        public GarageS3Service(IConfiguration config)
        {
            var section = config.GetSection("GarageS3");
            var endpoint = (section["Endpoint"] ?? throw new ArgumentNullException("GarageS3:Endpoint")).TrimEnd('/');
            var accessKey = section["AccessKey"] ?? throw new ArgumentNullException("GarageS3:AccessKey");
            var secretKey = section["SecretKey"] ?? throw new ArgumentNullException("GarageS3:SecretKey");
            _bucket = section["Bucket"] ?? throw new ArgumentNullException("GarageS3:Bucket");
            var region = section["Region"] ?? "garage";

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var s3Config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
                ForcePathStyle = true,
                AuthenticationRegion = region
            };

            _client = new AmazonS3Client(credentials, s3Config);
        }

        public async Task UploadPdfAsync(string key, Stream pdfStream, CancellationToken ct = default)
        {
            if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
            if (pdfStream.CanSeek) pdfStream.Position = 0;

            var req = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = pdfStream,
                ContentType = "application/pdf",
                UseChunkEncoding = false
            };

            if (pdfStream.CanSeek)
            {
                req.Headers.ContentLength = pdfStream.Length;
            }

            await _client.PutObjectAsync(req, ct);
        }

        public async Task<Stream> GetPdfAsync(string key, CancellationToken ct = default)
        {
            var resp = await _client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucket,
                Key = key
            }, ct);

            return resp.ResponseStream;
        }
    }
}
