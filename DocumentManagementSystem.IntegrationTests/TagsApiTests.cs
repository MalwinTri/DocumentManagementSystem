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

            // Die Antwort ist ein Array von Strings, daher prüfen wir, ob ein spezifisches Tag enthalten ist
            jsonResponse.Should().Contain("kw:Agile Testing Quadrants");
            jsonResponse.Should().Contain("kw:Funktionale Tests");
            jsonResponse.Should().Contain("kw:Teststrategie");

            // Optional: Prüfen, ob das Array die erwartete Anzahl an Elementen enthält
            var tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(jsonResponse);
            tags.Should().HaveCountGreaterThan(0, "Die Antwort sollte mindestens ein Tag enthalten.");
        }
    }
}
