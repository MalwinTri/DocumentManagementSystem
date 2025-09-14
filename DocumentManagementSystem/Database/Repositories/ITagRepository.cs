using DocumentManagementSystem.Domain;

namespace DocumentManagementSystem.Database.Repositories;

public interface ITagRepository
{
    Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default);
}
