using System.ComponentModel.DataAnnotations;

namespace DocumentManagementSystem.Models;

public class Document
{
    public Guid Id { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional: wann zuletzt verändert (UI, Auditing)
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? OcrText { get; set; }
    public DateTime? OcrCompletedAt { get; set; }   // Optional: wann OCR fertig war

    public string? Summary { get; set; }
    public DateTime? AiProcessedAt { get; set; }    // Optional: wann Summary/Metadata/Embeddings fertig

    // Optional: wann in Elasticsearch indexiert
    public DateTime? IndexedAt { get; set; }

    // NEW: Retry/Backoff für GenAI (gegen 429/Quota)
    public DateTime? AiNextAttemptAt { get; set; }
    public int AiAttempts { get; set; } = 0;
    public string? AiLastError { get; set; }

    public ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();

    // AI/Metadata Relations
    public DocumentMetadata? Metadata { get; set; }
    public ICollection<ExtractedEntity> ExtractedEntities { get; set; } = new List<ExtractedEntity>();
    public DocumentEmbedding? Embedding { get; set; }
}

