using System.ComponentModel.DataAnnotations;

namespace DocumentManagementSystem.Models;

public class Document
{
    public Guid Id { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();
}
