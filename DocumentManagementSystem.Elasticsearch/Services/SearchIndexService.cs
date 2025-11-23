using DocumentManagementSystem.Elasticsearch.Models;
using Elastic.Clients.Elasticsearch;

namespace DocumentManagementSystem.Elasticsearch.Services;

public class SearchIndexService : ISearchIndexService
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "documents";

    public SearchIndexService(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task IndexDocumentAsync(DocumentIndex document, CancellationToken ct = default)
    {
        var response = await _client.IndexAsync(document, r => r
            .Index(IndexName)
            .Id(document.Id), ct);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Failed to index document {document.Id}: {response.DebugInformation}");
        }
    }
}
