using DocumentManagementSystem.Domain;
using DocumentManagementSystem.Dto;

namespace DocumentManagementSystem.Mapping;

public static class DocumentMapper
{
    public static DocumentResponseDto ToDto(Document d) =>
        new(d.Id, d.Title, d.Description, d.Tags.Select(t => t.Name).ToList(), d.CreatedAt);
}
