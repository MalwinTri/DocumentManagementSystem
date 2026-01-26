using FluentAssertions;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace DocumentManagementSystem.IntegrationTests
{
    public class DocumentsApiUpdateTests : IClassFixture<DmsApiFactory>
    {
        private readonly HttpClient _client;

        public DocumentsApiUpdateTests(DmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UpdateDocument_ShouldReturnUpdatedDocument_WhenValidData()
        {
            // Erstellen Sie ein Dokument
            var documentId = await CreateTestDocument();

            // Erstellen Sie die neuen Daten
            var updateData = new
            {
                title = "Updated Document Title",
                description = "Updated description",
                tags = new[] { "updated", "test" }
            };

            var content = new StringContent(JsonConvert.SerializeObject(updateData), System.Text.Encoding.UTF8, "application/json");

            // Aktualisieren Sie das Dokument
            var updateResponse = await _client.PutAsync($"/api/documents/{documentId}", content);

            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedDocument = await updateResponse.Content.ReadAsStringAsync();
            updatedDocument.Should().Contain("Updated Document Title");
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

