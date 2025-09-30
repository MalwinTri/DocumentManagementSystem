using DocumentManagementSystem.Database;
using DocumentManagementSystem.Database.Repositories;
using DocumentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<DmsDbContext>(opt => opt.UseNpgsql(connectionString));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<DocumentService>();

const string AllowFrontend = "_allowFrontend";
builder.Services.AddCors(opts =>
{
    opts.AddPolicy(AllowFrontend, p => p
        .WithOrigins("http://localhost:5173", "http://localhost:3000") 
        .AllowAnyHeader()
        .AllowAnyMethod()
    // .AllowCredentials() // nur wenn du Cookies brauchst
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
    const int maxRetries = 5;
    for (var i = 1; i <= maxRetries; i++)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch when (i < maxRetries) { await Task.Delay(TimeSpan.FromSeconds(2 * i)); }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DMS v1"));
}

app.UseHttpsRedirection();              
app.UseCors(AllowFrontend);             
app.UseAuthorization();

app.MapControllers();
app.Run();
