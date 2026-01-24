namespace DocumentManagementSystem.Elasticsearch.Models;

public class DocumentIndex
{
    public string Id { get; set; } = string.Empty;      // Document.Id als String
    public string Title { get; set; } = string.Empty;   // Titel / Dateiname
    public string Content { get; set; } = string.Empty; // OCR-Text
    public string Summary { get; set; } = string.Empty; // GenAI-Summary
    public List<string> Tags { get; set; } = new();
    public DateTime UploadedAt { get; set; }
}
