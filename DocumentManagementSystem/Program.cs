using Serilog;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DocumentManagementSystem.Database;
using DocumentManagementSystem.Database.Repositories;
using DocumentManagementSystem.BL.Documents;
using DocumentManagementSystem.Middleware;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.Infrastructure.Services;
using DocumentManagementSystem.Infrastructure.Services.GenAI;

internal class Program
{
    private static async Task Main(string[] args)
    {
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


            // -- GenAI: Gemini -------

            builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));

            builder.Services.AddHttpClient<IGenAiService, GeminiService>();



            // ---- Normalize/Map config keys so either GARAGE_S3_* or S3_* works ----
            var map = new Dictionary<string, string?>();
            void Map(string target, string source)
            {
                var src = builder.Configuration[source];
                var dst = builder.Configuration[target];
                if (!string.IsNullOrWhiteSpace(src) && string.IsNullOrWhiteSpace(dst))
                    map[target] = src;
            }
            Map("S3_ENDPOINT", "GARAGE_S3_ENDPOINT");
            Map("S3_REGION", "GARAGE_S3_REGION");
            Map("S3_BUCKET", "GARAGE_S3_BUCKET");
            Map("S3_ACCESS_KEY", "GARAGE_S3_ACCESS_KEY");
            Map("S3_SECRET_KEY", "GARAGE_S3_SECRET_KEY");
            if (map.Count > 0) builder.Configuration.AddInMemoryCollection(map);

            // ---------- Database ----------
            var connectionString = builder.Configuration.GetConnectionString("Default");
            builder.Services.AddDbContext<DmsDbContext>(opt => opt.UseNpgsql(connectionString));

            builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
            builder.Services.AddScoped<ITagRepository, TagRepository>();
            builder.Services.AddScoped<DocumentService>();

            // ---------- RabbitMQ ----------
            // Als Interface registrieren; liest aus ENV oder appsettings (optional)
            builder.Services.AddSingleton<IRabbitMqService>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<RabbitMqService>>();
                var host = cfg["Rabbit:Host"] ?? cfg["RABBIT_HOST"] ?? "rabbitmq";
                var user = cfg["Rabbit:User"] ?? cfg["RABBIT_USER"] ?? "guest";
                var pass = cfg["Rabbit:Password"] ?? cfg["RABBIT_PASSWORD"] ?? "guest";
                var queue = cfg["Rabbit:Queue"] ?? cfg["RABBIT_QUEUE"] ?? "documents.ocr";
                return new RabbitMqService(logger, host, user, pass, queue);
            });

            // ---------- S3 / Garage ----------
            builder.Services.AddSingleton<IGarageS3Service, GarageS3Service>();

            // ---------- Upload size (z. B. 100 MB PDFs zulassen) ----------
            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
            });

            // ---------- CORS ----------
            const string AllowFrontend = "_allowFrontend";
            builder.Services.AddCors(opts =>
            {
                var origins = (builder.Configuration["FRONTEND_ORIGINS"]
                               ?? "http://localhost:5173;http://localhost:3000")
                              .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                opts.AddPolicy(AllowFrontend, p => p
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });

            // ---------- MVC + Swagger ----------
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

            // ---------- DB Migrations mit Retry ----------
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
    }
}
