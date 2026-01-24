using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DocumentManagementSystem.IntegrationTests;

public class DocumentsApiTests : IClassFixture<DmsApiFactory>
{
    private readonly HttpClient _client;

    public DocumentsApiTests(DmsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_then_GetById_should_return_document()
    {
        // Route laut Controller: [Route("api/[controller]")] + [HttpPost] => POST /api/documents
        const string url = "/api/documents";

        // Minimal "PDF" header reicht für ContentType checks (Service kann trotzdem validieren je nach Implementierung)
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"

        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "test.pdf"); // muss "file" heißen (errors["file"])

        // Title ist required (errors["title"])
        content.Add(new StringContent("Integration Test Document"), "title");

        // optional:
        content.Add(new StringContent("desc"), "description");
        // tags nur wenn dein Form es wirklich so erwartet (z.B. tags=... mehrfach)
        // content.Add(new StringContent("tag1"), "tags");
        // content.Add(new StringContent("tag2"), "tags");

        var uploadResp = await _client.PostAsync(url, content);

        uploadResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var uploadJson = await uploadResp.Content.ReadAsStringAsync();
        uploadJson.Should().NotBeNullOrWhiteSpace();

        using var uploadDoc = JsonDocument.Parse(uploadJson);
        var root = uploadDoc.RootElement;

        root.TryGetProperty("id", out var idProp).Should().BeTrue($"Response sollte 'id' enthalten. Response war: {uploadJson}");

        var idValue = idProp.GetString();
        Guid.TryParse(idValue, out var docId).Should().BeTrue($"id sollte GUID sein, war aber: {idValue}");

        // CreatedAtAction gibt Location Header normalerweise mit
        uploadResp.Headers.Location.Should().NotBeNull();

        // GET /api/documents/{id}
        var getResp = await _client.GetAsync($"/api/documents/{docId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResp.Content.ReadAsStringAsync();
        getJson.Should().NotBeNullOrWhiteSpace();

        using var getDoc = JsonDocument.Parse(getJson);
        var getRoot = getDoc.RootElement;

        getRoot.GetProperty("id").GetString().Should().Be(docId.ToString());
        getRoot.GetProperty("title").GetString().Should().Be("Integration Test Document");
    }
}
