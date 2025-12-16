using DocumentManagementSystem.Models;
using DocumentManagementSystem.Dto;

namespace DocumentManagementSystem.Mapping;

public static class DocumentMapper
{
    public static DocumentResponseDto ToDto(Document d) =>
        new(
            d.Id,
            d.Title,
            d.Description,
            d.Tags.Select(t => t.Name).ToList(),
            d.CreatedAt,
            d.OcrText,
            d.Summary,

            d.Metadata == null
                ? null
                : new DocumentMetadataDto(
                    d.Metadata.DocumentType,
                    d.Metadata.IssueDate,
                    d.Metadata.InvoiceNumber,
                    d.Metadata.Iban
                ),

            d.ExtractedEntities.Select(e => new ExtractedEntityDto(e.Type, e.Value)).ToList(),

            d.Embedding == null
                ? null
                : new DocumentEmbeddingDto(d.Embedding.Model, d.Embedding.Dims)
        );
}
