using System.Net;
using FluentAssertions;
using Xunit;

namespace DocumentManagementSystem.IntegrationTests
{
    public class DocumentsApiErrorTests : IClassFixture<DmsApiFactory>
    {
        private readonly HttpClient _client;

        public DocumentsApiErrorTests(DmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UploadDocument_ShouldReturnBadRequest_WhenMissingRequiredFields()
        {
            const string url = "/api/documents";

            var content = new MultipartFormDataContent(); // Keine Datei oder Titel

            var uploadResp = await _client.PostAsync(url, content);

            uploadResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteDocument_ShouldReturnNotFound_WhenDocumentDoesNotExist()
        {
            var nonExistentId = Guid.NewGuid();

            var deleteResponse = await _client.DeleteAsync($"/api/documents/{nonExistentId}");

            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
