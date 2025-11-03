using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace DocumentManagementSystem.Infrastructure.Services;

public class GarageS3Service
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;

    public GarageS3Service(IConfiguration config)
    {
        var section = config.GetSection("GarageS3");
        var endpoint = section["Endpoint"];
        var accessKey = section["AccessKey"];
        var secretKey = section["SecretKey"];
        _bucket = section["Bucket"];
        var region = section["Region"] ?? "garage"; // Add this line

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = region // Add this line
        };
        _client = new AmazonS3Client(credentials, s3Config);
    }

    public async Task UploadPdfAsync(string key, Stream pdfStream)
    {
        var req = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = pdfStream,
            ContentType = "application/pdf"
        };
        await _client.PutObjectAsync(req);
    }

    public async Task<Stream> GetPdfAsync(string key)
    {
        var req = new GetObjectRequest
        {
            BucketName = _bucket,
            Key = key
        };
        var resp = await _client.GetObjectAsync(req);
        return resp.ResponseStream;
    }
}