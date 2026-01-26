namespace DocumentManagementSystem.Models;

public class ExtractedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string Type { get; set; } = "";   // PERSON|ORG|LOCATION
    public string Value { get; set; } = "";
}

