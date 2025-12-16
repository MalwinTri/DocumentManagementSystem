namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    public interface IGenAiService
    {
        Task<string?> GenerateSummaryAsync(string text, CancellationToken cancellationToken = default);

        // NEU: strukturierte Extraktion (Metadaten + Entities + Keywords)
        Task<AiExtractionResult?> ExtractMetadataAsync(string text, CancellationToken cancellationToken = default);

        // NEU: Embedding-Vektor für semantische Suche
        Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    }
}
