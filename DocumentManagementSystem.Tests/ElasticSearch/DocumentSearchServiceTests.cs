using DocumentManagementSystem.Elasticsearch.Models;
using DocumentManagementSystem.Elasticsearch.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FluentAssertions;
using Moq;
using System.Net.Http;
using Xunit;

namespace DocumentManagementSystem.Tests.ElasticSearch;

public class DocumentSearchServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryIsNullOrWhitespace(string? query)
    {
        var client = new Mock<ElasticsearchClient>(MockBehavior.Strict);
        var sut = new DocumentSearchService(client.Object);

        var result = await sut.SearchAsync(query!);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchAsync_CallsElasticsearch_WhenQueryHasContent()
    {
        var client = new Mock<ElasticsearchClient>(MockBehavior.Loose);
        var sut = new DocumentSearchService(client.Object);

        client
            .Setup(c => c.SearchAsync<DocumentIndex>(It.IsAny<Action<SearchRequestDescriptor<DocumentIndex>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom"));

        var act = () => sut.SearchAsync("  hello  ");

        await act.Should().ThrowAsync<HttpRequestException>();

        client.Verify(
            c => c.SearchAsync<DocumentIndex>(
                It.IsAny<Action<SearchRequestDescriptor<DocumentIndex>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_PropagatesTransportException_FromClient()
    {
        var client = new Mock<ElasticsearchClient>(MockBehavior.Strict);
        var sut = new DocumentSearchService(client.Object);

        client
            .Setup(c => c.SearchAsync<DocumentIndex>(It.IsAny<Action<SearchRequestDescriptor<DocumentIndex>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransportException("transport failed"));

        var act = () => sut.SearchAsync("invoice");

        await act.Should().ThrowAsync<TransportException>();

        client.VerifyAll();
    }
}
