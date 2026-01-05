namespace DocumentManagementSystem.Dto;

public record ExtractedEntityDto(string Type, string Value);

public record DocumentMetadataDto(
    string DocumentType,
    DateOnly? IssueDate,
    string? InvoiceNumber,
    string? Iban
);

public record DocumentEmbeddingDto(
    string Model,
    int Dims
);

public record DocumentResponseDto(
    Guid Id,
    string Title,
    string? Description,
    List<string> Tags,
    DateTime CreatedAt,
    string? OcrText,
    string? Summary,

    DocumentMetadataDto? Metadata,
    List<ExtractedEntityDto> Entities,
    DocumentEmbeddingDto? Embedding
);
