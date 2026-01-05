namespace DocumentManagementSystem.Dto;

public sealed class SimilarDocumentResponseDto
{
    public required DocumentResponseDto Document { get; init; }
    public required double Score { get; init; } // 0..1 fürs UI
}
