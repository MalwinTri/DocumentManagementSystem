using DocumentManagementSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Database.Repositories;

public class TagRepository(DmsDbContext db) : ITagRepository
{
    public async Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim();
        var existing = await db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == n.ToLower(), ct);
        if (existing != null) return existing;

        var tag = new Tag { Name = n };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return tag;
    }
}
