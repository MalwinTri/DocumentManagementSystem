using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        public async Task SearchDocuments_ShouldReturnFilteredDocuments()
        {
            // Schritt 1: Ein Dokument erstellen
            var documentId = await CreateTestDocument();

            // Warten, damit Elasticsearch Zeit hat, das Dokument zu indizieren
            await Task.Delay(2000);  // 2 Sekunden Verzögerung

            // Schritt 2: Eine Suchanfrage durchführen (z.B. nach Titel filtern)
            var searchUrl = $"/api/documents/search?query=Test Document";
            var searchResponse = await _client.GetAsync(searchUrl);

            // Prüfen, ob die Antwort erfolgreich war
            searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Schritt 3: Überprüfen, ob das richtige Dokument zurückgegeben wurde
            var jsonResponse = await searchResponse.Content.ReadAsStringAsync();
            jsonResponse.Should().NotBeNullOrWhiteSpace();

            var documents = JsonSerializer.Deserialize<JsonArray>(jsonResponse);
            documents.Should().NotBeNull();
            documents.Count.Should().BeGreaterThan(0, "Die Suche sollte mindestens ein Dokument zurückgeben.");

            var firstDocument = documents[0].AsObject();
            if (firstDocument.TryGetPropertyValue("id", out var idNode))
            {
                var foundDocumentId = idNode.GetValue<Guid>();
                foundDocumentId.Should().Be(documentId, "Das gesuchte Dokument sollte zurückgegeben werden.");
            }
            else
            {
                throw new Exception("ID Property not found.");
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
            uploadResp.StatusCode.Should().Be(HttpStatusCode.Created);  // Sicherstellen, dass das Dokument erfolgreich erstellt wurde

            var uploadJson = await uploadResp.Content.ReadAsStringAsync();
            var uploadDoc = JsonDocument.Parse(uploadJson);
            var idProp = uploadDoc.RootElement.GetProperty("id");

            return Guid.Parse(idProp.GetString());
        }
    }
}
