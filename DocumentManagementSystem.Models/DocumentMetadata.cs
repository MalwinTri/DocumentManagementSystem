using System.Text.Json;

namespace DocumentManagementSystem.Models;

public class DocumentMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string DocumentType { get; set; } = "OTHER"; // INVOICE|CONTRACT|REMINDER|OTHER
    public DateOnly? IssueDate { get; set; }

    public string? InvoiceNumber { get; set; }
    public string? Iban { get; set; }

    // optional: raw json for debugging / flexibility
    public string RawJson { get; set; } = "{}";
}
