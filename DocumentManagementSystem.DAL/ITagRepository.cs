using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.DAL;

public interface ITagRepository
{
    Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SuggestAsync(string? q, int take = 20, CancellationToken ct = default);
}

