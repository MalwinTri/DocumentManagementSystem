namespace DocumentManagementSystem.Dto;

public sealed class DocumentUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }  
    public List<string>? Tags { get; set; }
    public string? Summary { get; set; }
}
