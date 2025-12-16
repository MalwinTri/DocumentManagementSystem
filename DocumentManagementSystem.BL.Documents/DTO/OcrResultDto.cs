namespace DocumentManagementSystem.Dto;

public sealed class OcrResultDto
{
    public Guid DocumentId { get; set; }
    public string OcrText { get; set; } = "";
}
