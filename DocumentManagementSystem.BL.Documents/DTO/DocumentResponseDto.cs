namespace DocumentManagementSystem.Dto;

public record DocumentResponseDto(
    Guid Id,
    string Title,
    string? Description,
    List<string> Tags,
    DateTime CreatedAt,
    string? OcrText,   
    string? Summary
);
