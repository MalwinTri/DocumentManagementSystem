using System.Text.RegularExpressions;
using DocumentManagementSystem.Database;
using DocumentManagementSystem.Database.Repositories;
using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public class TagRepository(DmsDbContext db) : ITagRepository
{
    public async Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default)
    {
        var n = Normalize(name);

        if (string.IsNullOrWhiteSpace(n))
        {
            throw new ValidationException(
                code: "validation_error",
                detail: "Tag name is required.",
                errors: new Dictionary<string, string[]> { ["name"] = new[] { "Tag name is required." } }
            );
        }

        var tag = new Tag { Name = n };
        db.Tags.Add(tag);

        try
        {
            await db.SaveChangesAsync(ct); 
            return tag;                    
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                           pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            db.Entry(tag).State = EntityState.Detached;

            var loaded = await db.Tags.FirstOrDefaultAsync(t => EF.Functions.ILike(t.Name, n), ct);
            if (loaded is not null) return loaded;

            throw new UniqueConstraintViolationException(
                constraintName: pg.ConstraintName,
                value: new { field = "name", value = n },
                entity: nameof(Tag),
                detail: pg.Detail,
                inner: ex
            );
        }
        catch (DbUpdateException ex)
        {
            throw new RepositoryException(
                message: "DB update failed",
                operation: "save_changes",
                entity: nameof(Tag),
                inner: ex
            );
        }
    }

    private static string Normalize(string? input)
    {
        var s = (input ?? string.Empty).Trim();
        if (s.Length == 0) return s;

        s = Regex.Replace(s, @"\s+", " ");

        return s;
    }
}
