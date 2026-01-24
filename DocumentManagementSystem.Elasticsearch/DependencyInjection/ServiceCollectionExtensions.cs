using DocumentManagementSystem.Elasticsearch.Configuration;
using DocumentManagementSystem.Elasticsearch.Services;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.Elasticsearch.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElasticsearchServices(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        services.Configure<ElasticsearchOptions>(options =>
        {
            options.Uri = configuration["Elasticsearch:Uri"];
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Uri))
            {
                throw new InvalidOperationException(
                    "Missing configuration 'Elasticsearch:Uri'. " +
                    "Set it in appsettings.json oder als Environment Variable 'Elasticsearch__Uri'.");
            }

            var settings = new ElasticsearchClientSettings(new Uri(options.Uri))
                .DefaultIndex("documents");

            return new ElasticsearchClient(settings);
        });

        services.AddSingleton<ISearchIndexService, SearchIndexService>();
        services.AddScoped<IDocumentSearchService, DocumentSearchService>();

        return services;
    }

}
