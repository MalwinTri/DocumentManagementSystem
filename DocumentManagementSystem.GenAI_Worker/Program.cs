using DocumentManagementSystem.Database;
using DocumentManagementSystem.GenAI_Worker.AiWorker;
using DocumentManagementSystem.Infrastructure.Services.GenAI;
using DocumentManagementSystem.Elasticsearch.DependencyInjection;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // DbContext (same connection string name as API)
        services.AddDbContext<DmsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Gemini options + service
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.AddHttpClient<IGenAiService, GeminiService>();

        // Elasticsearch-Dienste (gleiche Config wie im API)
        services.AddElasticsearchServices(configuration);

        // Background worker
        services.AddHostedService<GenAiWorkerService>();
    })
    .Build();

await host.RunAsync();
