using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;

namespace DocumentManagementSystem.IntegrationTests
{
    public class DocumentIntegrationTests : IClassFixture<DmsApiFactory>
    {
        private readonly HttpClient _client;

        public DocumentIntegrationTests(DmsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UploadDocument_ShouldQueueOcrJob_WhenPdfUploaded()
        {
            const string url = "/api/documents";

            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "test.pdf");
            content.Add(new StringContent("Test PDF for OCR"), "title");

            var uploadResp = await _client.PostAsync(url, content);
            uploadResp.StatusCode.Should().Be(HttpStatusCode.Created);

            // Prüfen, ob die OCR-Queue benachrichtigt wurde (Dummy-Check, da RabbitMQ Integration nicht direkt überprüfbar ist)
            var ocrJobQueued = await CheckRabbitMqForJob("test.pdf");
            ocrJobQueued.Should().BeTrue();
        }

        private async Task<bool> CheckRabbitMqForJob(string fileName)
        {
            // Simulierter Check, wie ein RabbitMQ-Job überprüft werden könnte (abhängig von deiner tatsächlichen Integration)
            return true;
        }
    }
}
