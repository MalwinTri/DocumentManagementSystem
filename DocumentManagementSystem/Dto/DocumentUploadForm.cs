using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DocumentManagementSystem.Dto;

// Repräsentiert das multipart/form-data Formular für den Upload
public class DocumentUploadForm
{
    [Required]                        // zwingend
    public IFormFile File { get; set; } = default!;

    [Required, MaxLength(255)]        // Titel nötig, max 255 Zeichen
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Mehrfach übermittelbare Felder "tags"
    public List<string>? Tags { get; set; }
}
