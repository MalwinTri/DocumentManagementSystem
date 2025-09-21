using DocumentManagementSystem.Database;
using DocumentManagementSystem.Database.Repositories;
using DocumentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

foreach (var env in Environment.GetEnvironmentVariables().Keys)
{
    Console.WriteLine($"[ENV] {env} = {Environment.GetEnvironmentVariable(env.ToString())}");
}

var connectionString = builder.Configuration.GetConnectionString("Default");
Console.WriteLine($"[DEBUG] ConnectionString: {connectionString}");
builder.Services.AddDbContext<DmsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<DocumentService>();

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
app.UseAuthorization();
app.MapControllers();
app.Run();