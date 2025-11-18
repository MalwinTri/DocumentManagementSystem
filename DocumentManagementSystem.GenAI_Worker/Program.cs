using DocumentManagementSystem.Database;
using DocumentManagementSystem.GenAI_Worker.AiWorker;
using DocumentManagementSystem.Infrastructure.Services.GenAI;
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

        // Background worker
        services.AddHostedService<GenAiWorkerService>();
    })
    .Build();

await host.RunAsync();
