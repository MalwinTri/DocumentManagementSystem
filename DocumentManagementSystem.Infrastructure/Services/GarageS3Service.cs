using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DocumentManagementSystem.Infrastructure.Services
{
    public class GarageS3Service
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
            var region = section["Region"] ?? "garage";   // muss zu s3_region in garage.toml passen

            var credentials = new BasicAWSCredentials(accessKey, secretKey);

            var s3Config = new AmazonS3Config
            {
                ServiceURL = endpoint,                                // z.B. http://garage:3900
                UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
                ForcePathStyle = true,                                // /bucket/key statt bucket.host
                AuthenticationRegion = region                         // "garage" (oder was in garage.toml steht)
            };

            // optional (falls vorhanden in deiner SDK-Version; sonst einfach weglassen):
            // Amazon.S3.Util.AWSConfigsS3.UseSignatureVersion4 = true;

            _client = new AmazonS3Client(credentials, s3Config);
        }

        public async Task UploadPdfAsync(string key, Stream pdfStream)
        {
            if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));
            if (pdfStream.CanSeek) pdfStream.Position = 0;

            var req = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = pdfStream,
                ContentType = "application/pdf",
                UseChunkEncoding = false   // <-- wichtig: kein aws-chunked
            };

            if (pdfStream.CanSeek)
            {
                req.Headers.ContentLength = pdfStream.Length; // feste Länge helfen Signaturproblemen vorzubeugen
            }

            await _client.PutObjectAsync(req);
        }

        public async Task<Stream> GetPdfAsync(string key)
        {
            var resp = await _client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucket,
                Key = key
            });
            return resp.ResponseStream;
        }
    }
}
