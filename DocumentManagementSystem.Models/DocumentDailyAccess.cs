namespace DocumentManagementSystem.Models;

public sealed class DocumentDailyAccess
{
    public Guid DocumentId { get; set; }
    public DateOnly Day { get; set; }
    public int Count { get; set; }
    public Document Document { get; set; } = null!;
}
