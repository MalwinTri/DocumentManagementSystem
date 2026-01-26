using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class DmsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Wichtig: Host+Port mÃ¼ssen zum docker ps passen (bei dir 5433)
        Environment.SetEnvironmentVariable("ConnectionStrings__Default",
            "Host=localhost;Port=5433;Database=dms;Username=postgres;Password=postgres");

        Environment.SetEnvironmentVariable("GarageS3__Endpoint", "http://localhost:3900");
        Environment.SetEnvironmentVariable("Elasticsearch__Uri", "http://localhost:9200");

        Environment.SetEnvironmentVariable("GarageS3__AccessKey", "GK2753898fcf6b6f8f6f7bd017");
        Environment.SetEnvironmentVariable("GarageS3__SecretKey", "2b5e16bd07682cdf44be6024294d79eae43a47ccd8d4c9c5a19c01bdd5d81567");
        Environment.SetEnvironmentVariable("GarageS3__Bucket", "documents");
        Environment.SetEnvironmentVariable("GarageS3__Region", "garage");

        // falls RabbitMQ wirklich gebraucht wird:
        Environment.SetEnvironmentVariable("RABBIT_HOST", "localhost");
        Environment.SetEnvironmentVariable("RABBIT_USER", "guest");
        Environment.SetEnvironmentVariable("RABBIT_PASSWORD", "guest");
        Environment.SetEnvironmentVariable("RABBIT_QUEUE", "ocr-queue");
    }
}

