using System.ComponentModel.DataAnnotations;

namespace DocumentManagementSystem.Models;

public class Tag
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Document> Documents { get; set; } = new HashSet<Document>();
}
