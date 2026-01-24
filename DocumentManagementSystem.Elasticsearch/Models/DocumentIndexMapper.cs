using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.Elasticsearch.Models;

public static class DocumentIndexMapper
{
    public static DocumentIndex ToDocumentIndex(this Document document)
    {
        return new DocumentIndex
        {
            Id = document.Id.ToString(),
            Title = document.Title,
            Content = document.OcrText ?? string.Empty,
            Summary = document.Summary ?? string.Empty,
            Tags = document.Tags
                .Select(t => t.Name)
                .ToList(),
            UploadedAt = document.CreatedAt
        };
    }
}
