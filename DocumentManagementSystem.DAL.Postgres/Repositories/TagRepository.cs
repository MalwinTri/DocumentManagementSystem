using DocumentManagementSystem.DAL;
using DocumentManagementSystem.DAL.Postgres.Exceptions;       
using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.RegularExpressions;

namespace DocumentManagementSystem.Database.Repositories;

public class TagRepository(DmsDbContext db, ILogger<TagRepository> logger) : ITagRepository
{
    private readonly DmsDbContext _db = db;
    private readonly ILogger<TagRepository> _logger = logger;

    public async Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default)
    {
        // DAL wirft keine BL-Exceptions -> Guard als ArgumentException
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name is required.", nameof(name));

        var normalized = Normalize(name);
        var normalizedLower = normalized.ToLower();

        _logger.LogDebug("GetOrCreateAsync called for tag='{TagName}'", normalized);

        // READ: no tracking, case-insensitive Vergleich via ToLower() (EF übersetzt zu LOWER(name)=...)
        var existing = await _db.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedLower, ct);

        if (existing is not null)
        {
            _logger.LogDebug("Tag exists: {TagName} (Id={Id})", existing.Name, existing.Id);
            return existing;
        }

        // CREATE
        var tag = new Tag { Name = normalized };
        _db.Tags.Add(tag);

        try
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Created new tag '{TagName}' (Id={Id})", tag.Name, tag.Id);
            return tag;
        }
        // Unique-Verletzung -> versuche bestehenden Datensatz erneut zu laden
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            _db.Entry(tag).State = EntityState.Detached;

            var loaded = await _db.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedLower, ct);

            if (loaded is not null) return loaded;

            throw new UniqueConstraintViolationException(
                constraintName: pg.ConstraintName,
                value: new { field = "name", value = normalized },
                entity: nameof(Tag),
                detail: pg.Detail,
                inner: ex
            );
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DB update failed while creating tag '{TagName}'", normalized);
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
