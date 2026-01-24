using DocumentManagementSystem.Elasticsearch.Models;
using Elastic.Clients.Elasticsearch;

namespace DocumentManagementSystem.Elasticsearch.Services;

public class DocumentSearchService : IDocumentSearchService
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "documents";

    public DocumentSearchService(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyCollection<DocumentIndex>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        var trimmed = query?.Trim();

        // Nur leere / whitespace-Queries blocken
        if (string.IsNullOrWhiteSpace(trimmed))
            return Array.Empty<DocumentIndex>();

        var response = await _client.SearchAsync<DocumentIndex>(s => s
            .Indices(IndexName)
            .Query(q => q
                .Bool(b => b
                    .Should(
                        // 1) Wildcard nur auf dem Titel (Teilwortsuche)
                        should => should.Wildcard(w => w
                            .Field("title.keyword")
                            .Wildcard($"*{trimmed}*")
                        ),
                        // 2) Fuzzy-Match auf Title
                        should => should.Match(m => m
                            .Field(f => f.Title)
                            .Query(trimmed)
                            .Fuzziness(new Fuzziness("AUTO"))
                        )
                    )
                    .MinimumShouldMatch(1)
                )
            ),
            ct);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                "Elasticsearch search failed: " + response.DebugInformation);
        }

        return response.Hits
            .Where(h => h.Source is not null)
            .Select(h => h.Source!)
            .ToList();
    }
}
