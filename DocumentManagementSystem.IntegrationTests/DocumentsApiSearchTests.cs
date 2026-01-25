using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace DocumentManagementSystem.IntegrationTests
{
    public class DocumentsApiSearchTests : IClassFixture<DmsApiFactory>
    {
        private readonly HttpClient _client;

        public DocumentsApiSearchTests(DmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SearchDocuments_ShouldReturnEmpty_WhenDocumentNotIndexedYet()
        {
            _ = await CreateTestDocument();

            var resp = await _client.GetAsync("/api/documents/search?query=Test%20Document");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await resp.Content.ReadAsStringAsync();
            var arr = JsonSerializer.Deserialize<JsonArray>(json);

            arr.Should().NotBeNull();
            arr!.Count.Should().Be(0, "ohne OCR/GenAI-Indexing ist Elasticsearch noch leer.");
        }

        private async Task<Guid> CreateTestDocument()
        {
            const string url = "/api/documents";

            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "test.pdf");
            content.Add(new StringContent("Test Document"), "title");

            var uploadResp = await _client.PostAsync(url, content);
            uploadResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var uploadJson = await uploadResp.Content.ReadAsStringAsync();
            var uploadDoc = JsonDocument.Parse(uploadJson);
            var idProp = uploadDoc.RootElement.GetProperty("id");

            return Guid.Parse(idProp.GetString()!);
        }
    }
}
