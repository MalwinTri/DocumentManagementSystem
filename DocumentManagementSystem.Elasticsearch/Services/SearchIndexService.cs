using DocumentManagementSystem.Elasticsearch.Models;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System.Net.Http;

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
        const int maxAttempts = 6;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await _client.IndexAsync(document, r => r
                    .Index(IndexName)
                    .Id(document.Id), ct);

                if (!response.IsValidResponse)
                {
                    throw new InvalidOperationException(
                        $"Failed to index document {document.Id}: {response.DebugInformation}");
                }

                return; // success
            }
            catch (HttpRequestException) when (attempt < maxAttempts)
            {
                // connection refused / ES not ready yet
            }
            catch (TransportException) when (attempt < maxAttempts)
            {
                // transport layer failed (connect, timeouts, etc.)
            }

            // Exponential backoff: 2s,4s,8s,16s,30s...
            var delaySeconds = Math.Min(30, (int)Math.Pow(2, attempt));
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
        }

        throw new InvalidOperationException(
            $"Failed to index document {document.Id} after {maxAttempts} attempts (Elasticsearch not reachable).");
    }
}
