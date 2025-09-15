using DocumentManagementSystem.Database;                 // DbContext
using DocumentManagementSystem.Database.Repositories;    // Repos
using DocumentManagementSystem.Services;                 // Service
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- DB: PostgreSQL anbinden (ConnectionString kommt aus appsettings.json) ---
builder.Services.AddDbContext<DmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- DI: Repositories + Service registrieren ---
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<DocumentService>();

// --- MVC / Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();   // bei Docker oft entfernen/auskommentieren
app.UseAuthorization();

app.MapControllers();

app.Run();
