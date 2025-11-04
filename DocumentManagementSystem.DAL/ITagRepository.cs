using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.DAL;

public interface ITagRepository
{
    Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default);
}
