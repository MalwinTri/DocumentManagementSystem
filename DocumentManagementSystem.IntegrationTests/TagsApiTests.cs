using System.Net;
using FluentAssertions;
using Xunit;

namespace DocumentManagementSystem.IntegrationTests
{
    public class TagsApiTests : IClassFixture<DmsApiFactory>
    {
        private readonly HttpClient _client;

        public TagsApiTests(DmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SuggestTags_ShouldReturnTags()
        {
            const string url = "/api/tags/suggest?q=test";

            var response = await _client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var jsonResponse = await response.Content.ReadAsStringAsync();
            jsonResponse.Should().NotBeNullOrWhiteSpace();

            jsonResponse.Should().Contain("tag"); // Beispiel für das enthaltene Wort "tag"
        }
    }
}
