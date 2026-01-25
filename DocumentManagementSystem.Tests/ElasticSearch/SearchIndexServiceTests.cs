using DocumentManagementSystem.Elasticsearch.Models;
using DocumentManagementSystem.Elasticsearch.Services;
using Elastic.Clients.Elasticsearch;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DocumentManagementSystem.Tests.ElasticSearch;

public class SearchIndexServiceTests
{
    private static readonly Uri ElasticsearchUri =
        new(Environment.GetEnvironmentVariable("ELASTICSEARCH_URL") ?? "http://localhost:9200");

    private static ElasticsearchClient CreateClient()
    {
        var settings = new ElasticsearchClientSettings(ElasticsearchUri)
            .DisableDirectStreaming()
            .RequestTimeout(TimeSpan.FromSeconds(10));

        return new ElasticsearchClient(settings);
    }

    [Fact]
    public async Task IndexDocumentAsync_IndexesAndReturns_WhenResponseIsValid()
    {
        var doc = new DocumentIndex { Id = $"doc-{Guid.NewGuid():N}", Title = "T" };

        var client = CreateClient();
        var sut = new SearchIndexService(client);

        await sut.IndexDocumentAsync(doc);

        const string indexName = "documents";
        var get = await client.GetAsync<DocumentIndex>(doc.Id, g => g.Index(indexName));
        get.IsValidResponse.Should().BeTrue();
        get.Found.Should().BeTrue();
    }

    [Fact]
    public async Task IndexDocumentAsync_ThrowsAfterMaxAttempts_WhenElasticsearchIsNotReachable()
    {
        var badClient = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri("http://localhost:1")));
        var sut = new SearchIndexService(badClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var ex = await FluentActions.Awaiting(() => sut.IndexDocumentAsync(
                new DocumentIndex { Id = "doc-1", Title = "T" },
                cts.Token))
            .Should().ThrowAsync<Exception>();

        ex.Which.Should().Match<Exception>(e =>
            e is OperationCanceledException ||
            (e.GetType() == typeof(InvalidOperationException) &&
             e.Message.Contains("Failed to index document", StringComparison.OrdinalIgnoreCase)));
    }
}

