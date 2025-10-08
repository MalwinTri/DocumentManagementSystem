using System.Text.RegularExpressions;
using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentManagementSystem.Database.Repositories;

public class TagRepository(DmsDbContext db, ILogger<TagRepository> logger) : ITagRepository
{
    private readonly DmsDbContext _db = db;
    private readonly ILogger<TagRepository> _logger = logger;

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

        _logger.LogDebug("GetOrCreateAsync called for tag='{TagName}'", n);

        // Bestehenden Tag (case-insensitive) suchen
        var existing = await _db.Tags
            .FirstOrDefaultAsync(t => EF.Functions.ILike(t.Name, n), ct);
        if (existing is not null)
        {
            _logger.LogDebug("Tag exists: {TagName} (Id={Id})", existing.Name, existing.Id);
            return existing;
        }

        var tag = new Tag { Name = n };
        _db.Tags.Add(tag);

        try
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Created new tag '{TagName}' (Id={Id})", tag.Name, tag.Id);
            return tag;
        }
        // Postgres: 23505 = unique_violation
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                           pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Lokales Entity ablösen und erneut laden (race condition)
            _db.Entry(tag).State = EntityState.Detached;

            var loaded = await _db.Tags.FirstOrDefaultAsync(t => EF.Functions.ILike(t.Name, n), ct);
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
            _logger.LogError(ex, "DB update failed while creating tag '{TagName}'", n);
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

        // Mehrfache Whitespaces zu einem Space zusammenfassen
        s = Regex.Replace(s, @"\s+", " ");

        return s;
    }
}
