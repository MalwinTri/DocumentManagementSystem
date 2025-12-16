namespace DocumentManagementSystem.Models;

public class DocumentEmbedding
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string Model { get; set; } = "gemini-embedding";
    public int Dims { get; set; } = 768;

    // simplest persistence: json string with float array
    public string VectorJson { get; set; } = "[]";
}
