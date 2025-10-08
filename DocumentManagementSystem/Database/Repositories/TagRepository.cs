using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

using DocumentManagementSystem.Exceptions;


namespace DocumentManagementSystem.Database.Repositories;

public class TagRepository(DmsDbContext db) : ITagRepository
{
    public async Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim();


        var existing = await db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == n.ToLower(), ct);
        if (existing != null) return existing;

        var tag = new Tag { Name = n };



        try
        {
            db.Tags.Add(tag);
            await db.SaveChangesAsync(ct);
            return tag;
        }

        catch (DbUpdateException dbx)
        {
            // Same “starter” uniqueness detection as in DocumentRepository.
            // Later (when you add a real unique index on Tag.Name), you can check
            // Npgsql’s SqlState == "23505" for exact detection.
            if (dbx.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
                || dbx.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
            {
                throw new UniqueConstraintViolationException(inner: dbx);
            }

            throw new RepositoryException(inner: dbx);
        }
    }
}
