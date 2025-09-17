using DocumentManagementSystem.Database;                 // DbContext
using DocumentManagementSystem.Database.Repositories;    // Repositories
using DocumentManagementSystem.Services;                 // Services
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- DB: PostgreSQL anbinden (ConnectionString aus appsettings.json) ---
builder.Services.AddDbContext<DmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- DI: Repositories + Services registrieren ---
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<DocumentService>();

// --- MVC + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DocumentManagementSystem", Version = "v1" });
    // vermeidet Schema-Namenskonflikte in Swagger
    c.CustomSchemaIds(t => t.FullName);
});

var app = builder.Build();

// --- DB: Automatisch Migrationen anwenden (legt DB/Tabellen an) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();

    // kleiner Retry, wenn der DB-Container noch startet
    const int maxRetries = 5;
    for (var i = 1; i <= maxRetries; i++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch when (i < maxRetries)
        {
            await Task.Delay(TimeSpan.FromSeconds(2 * i));
        }
    }
}

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DMS v1");
    });
}

// In Docker ggf. auskommentieren, wenn nur HTTP genutzt wird
app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();
