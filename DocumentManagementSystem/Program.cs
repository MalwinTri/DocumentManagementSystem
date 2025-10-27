using Serilog;
using DocumentManagementSystem.Database;
using DocumentManagementSystem.Database.Repositories;
using DocumentManagementSystem.Services;
using DocumentManagementSystem.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables();

    builder.Host.UseSerilog();

    var connectionString = builder.Configuration.GetConnectionString("Default");
    builder.Services.AddDbContext<DmsDbContext>(opt => opt.UseNpgsql(connectionString));

    builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
    builder.Services.AddScoped<ITagRepository, TagRepository>();
    builder.Services.AddScoped<DocumentService>();

    // === RabbitMQ: RabbitMqService für DI registrieren ===
    var rabbitHost = builder.Configuration["RABBIT_HOST"] ?? "rabbitmq";
    var rabbitQueue = builder.Configuration["RABBIT_QUEUE"] ?? "ocr-queue"; // optional, falls du es später brauchst
    builder.Services.AddSingleton<RabbitMqService>(_ =>
        new RabbitMqService(rabbitHost) // nutzt deinen bestehenden ctor (nur hostName)
    );
    // =====================================================

    const string AllowFrontend = "_allowFrontend";

    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy(AllowFrontend, p => p
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
        );
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "DocumentManagementSystem", Version = "v1" });
        c.CustomSchemaIds(t => t.FullName);
    });

    var app = builder.Build();

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting application in environment {Env}", app.Environment.EnvironmentName);

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
        const int maxRetries = 5;
        for (var i = 1; i <= maxRetries; i++)
        {
            try
            {
                logger.LogInformation("Applying migrations (attempt {Attempt})", i);
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migration applied");
                break;
            }
            catch (Exception ex) when (i < maxRetries)
            {
                logger.LogWarning(ex, "Migration attempt {Attempt} failed, will retry", i);
                await Task.Delay(TimeSpan.FromSeconds(2 * i));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migrations failed after {Attempt} attempts", i);
                throw;
            }
        }
    }

    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DMS v1"));
    }

    app.UseCors(AllowFrontend);

    if (!app.Environment.IsEnvironment("Docker"))
    {
        app.UseHttpsRedirection();
    }

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseAuthorization();
    app.MapControllers();

    logger.LogInformation("Application started and listening");
    app.Run();
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
