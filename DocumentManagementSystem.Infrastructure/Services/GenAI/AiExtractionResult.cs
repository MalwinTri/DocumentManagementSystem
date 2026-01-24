using System.Text.Json.Serialization;

namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    public sealed class AiExtractionResult
    {
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = "OTHER"; // INVOICE|CONTRACT|REMINDER|OTHER

        [JsonPropertyName("issueDate")]
        public string? IssueDate { get; set; } // "YYYY-MM-DD" oder null

        [JsonPropertyName("invoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("iban")]
        public string? Iban { get; set; }

        [JsonPropertyName("persons")]
        public List<string> Persons { get; set; } = new();

        [JsonPropertyName("organizations")]
        public List<string> Organizations { get; set; } = new();

        [JsonPropertyName("locations")]
        public List<string> Locations { get; set; } = new();

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();

        // optional: raw JSON zum Debuggen in DB speichern
        public string RawJson { get; set; } = "{}";
    }
}
