using FluentAssertions;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DocumentManagementSystem.IntegrationTests
{
    public class DocumentsApiBulkDeleteTests : IClassFixture<DmsApiFactory>
    {
        private readonly HttpClient _client;

        public DocumentsApiBulkDeleteTests(DmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task BulkDeleteDocuments_ShouldDeleteMultipleDocuments()
        {
            // Erstellen Sie mehrere Dokumente
            var documentIds = new List<Guid>
            {
                await CreateTestDocument(),
                await CreateTestDocument()
            };

            // FÃ¼hren Sie die Bulk-LÃ¶schoperation aus
            var response = await _client.PostAsJsonAsync("/api/documents/bulk-delete", documentIds);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // ÃœberprÃ¼fen Sie, ob alle Dokumente gelÃ¶scht wurden
            foreach (var docId in documentIds)
            {
                var getResponse = await _client.GetAsync($"/api/documents/{docId}");
                getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            }
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

