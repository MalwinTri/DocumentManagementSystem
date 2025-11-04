using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DocumentManagementSystem.Dto;

public class DocumentUploadForm
{
    [Required]                       
    public IFormFile File { get; set; } = default!;

    [Required, MaxLength(255)]        
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string>? Tags { get; set; }
}
