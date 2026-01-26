using DocumentManagementSystem.AccessBatch.Options;
using DocumentManagementSystem.AccessBatch.Worker;
using DocumentManagementSystem.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog((ctx, services, cfg) =>
                {
                    cfg.Enrich.FromLogContext()
                       .WriteTo.Console();
                })
                .ConfigureServices((ctx, services) =>
                {
                    // Options aus appsettings.json / env
                    services.Configure<AccessBatchOptions>(ctx.Configuration.GetSection("AccessBatch"));

                    // DB
                    var cs =
                        ctx.Configuration.GetConnectionString("Default")
                        ?? Environment.GetEnvironmentVariable("DMS_CONNECTION_STRING");

                    if (string.IsNullOrWhiteSpace(cs))
                        throw new InvalidOperationException("No DB connection string found.");

                    services.AddDbContext<DmsDbContext>(o => o.UseNpgsql(cs));

                    // Worker
                    services.AddHostedService<AccessBatchWorker>();
                })
                .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

