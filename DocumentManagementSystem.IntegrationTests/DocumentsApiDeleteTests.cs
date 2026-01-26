using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DocumentManagementSystem.IntegrationTests
{
    public class DocumentsApiDeleteTests : IClassFixture<DmsApiFactory>
    {
        private readonly HttpClient _client;

        public DocumentsApiDeleteTests(DmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task DeleteDocument_ShouldReturnNoContent_WhenDocumentExists()
        {
            // Erstellen Sie ein Dokument, um es spÃ¤ter zu lÃ¶schen
            var documentId = await CreateTestDocument();

            // LÃ¶schen Sie das Dokument
            var deleteResponse = await _client.DeleteAsync($"/api/documents/{documentId}");

            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // ÃœberprÃ¼fen Sie, ob das Dokument wirklich gelÃ¶scht wurde
            var getResponse = await _client.GetAsync($"/api/documents/{documentId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private async Task<Guid> CreateTestDocument()
        {
            const string url = "/api/documents";

            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "test.pdf");
            content.Add(new StringContent("Test Document"), "title");

            var uploadResp = await _client.PostAsync(url, content);
            var uploadJson = await uploadResp.Content.ReadAsStringAsync();
            var uploadDoc = JsonDocument.Parse(uploadJson);
            var idProp = uploadDoc.RootElement.GetProperty("id");

            return Guid.Parse(idProp.GetString());
        }
    }
}

