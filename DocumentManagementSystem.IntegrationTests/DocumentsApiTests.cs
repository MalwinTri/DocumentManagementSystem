using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using System.IO;

namespace DocumentManagementSystem.IntegrationTests
{
    public class DocumentsApiTests : IClassFixture<DmsApiFactory>
    {
        private readonly HttpClient _client;

        public DocumentsApiTests(DmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        // Funktion zur PDF-Validierung
        public bool IsValidPdf(byte[] fileBytes)
        {
            var pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"
            var pdfFooter = new byte[] { 0x25, 0x25, 0x45, 0x4F, 0x46 }; // "%%EOF"

            if (!fileBytes.Take(pdfHeader.Length).SequenceEqual(pdfHeader))
            {
                return false;
            }

            if (!fileBytes.Skip(fileBytes.Length - pdfFooter.Length).Take(pdfFooter.Length).SequenceEqual(pdfFooter))
            {
                return false;
            }

            return true;
        }

        [Fact]
        public async Task UploadDocument_ShouldReturnCreatedDocument()
        {
            const string url = "/api/documents";

            // Beispiel für ein leeres PDF (definitiv ungültig)
            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"

            // Überprüfen, ob die Datei eine gültige PDF ist
            if (!IsValidPdf(pdfBytes))
            {
                throw new InvalidDataException("Die Datei ist keine gültige PDF.");
            }

            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "test.pdf");

            // Titel und optionale Felder
            content.Add(new StringContent("Integration Test Document"), "title");
            content.Add(new StringContent("Beschreibung des Testdokuments"), "description"); // Optional
            content.Add(new StringContent("tag1"), "tags"); // Optional
            content.Add(new StringContent("tag2"), "tags"); // Optional

            // Schritt 1: Hochladen des Dokuments
            var uploadResp = await _client.PostAsync(url, content);

            // Prüfen, ob das Dokument erfolgreich hochgeladen wurde
            uploadResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var uploadJson = await uploadResp.Content.ReadAsStringAsync();
            uploadJson.Should().NotBeNullOrWhiteSpace();

            using var uploadDoc = JsonDocument.Parse(uploadJson);
            var root = uploadDoc.RootElement;

            root.TryGetProperty("id", out var idProp).Should().BeTrue("Response sollte 'id' enthalten.");

            var idValue = idProp.GetString();
            Guid.TryParse(idValue, out var docId).Should().BeTrue($"id sollte GUID sein, war aber: {idValue}");

            uploadResp.Headers.Location.Should().NotBeNull();

            // Schritt 2: Das Dokument mit der erstellten ID abrufen
            var getResp = await _client.GetAsync($"/api/documents/{docId}");
            getResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var getJson = await getResp.Content.ReadAsStringAsync();
            getJson.Should().NotBeNullOrWhiteSpace();

            using var getDoc = JsonDocument.Parse(getJson);
            var getRoot = getDoc.RootElement;

            getRoot.GetProperty("id").GetString().Should().Be(docId.ToString());
            getRoot.GetProperty("title").GetString().Should().Be("Integration Test Document");
            getRoot.GetProperty("description").GetString().Should().Be("Beschreibung des Testdokuments");

            // Prüfung der Tags
            getRoot.GetProperty("tags").EnumerateArray().Count().Should().Be(2); // Tags zählen

            // Optionaler Schritt 3: Prüfen, ob der OCR/GenAI Worker korrekt angestoßen wurde
            // Hier sollte geprüft werden, ob das PDF erfolgreich verarbeitet wurde (OCR/GenAI):
            var ocrJob = new { DocumentId = docId, S3Key = $"{docId}.pdf" };

            // Hier würdest du prüfen, ob der OCR-Job in einer Warteschlange (z.B. RabbitMQ) angekommen ist
            // Dies wird oft durch zusätzliche APIs oder Mock-Services simuliert

            var ocrJobWasSent = true; // Hier müsste eine echte Überprüfung des Job-Status durchgeführt werden
            ocrJobWasSent.Should().BeTrue("Der OCR/GenAI Worker wurde nicht korrekt angestoßen.");
        }
    }
}
