using DocumentManagementSystem.DAL;
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
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name is required.", nameof(name));

        var normalized = Normalize(name);

        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("Tag name is required.", nameof(name));

        if (normalized.Length > 64)
            normalized = normalized[..64];

        _logger.LogDebug("GetOrCreateAsync called for tag='{TagName}'", normalized);

        var existing = await _db.Tags.FirstOrDefaultAsync(t => t.Name == normalized, ct);

        if (existing is not null)
        {
            _logger.LogDebug("Tag exists: {TagName} (Id={Id})", existing.Name, existing.Id);
            return existing;
        }

        var tag = new Tag { Name = normalized };
        _db.Tags.Add(tag);

        try
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Created new tag '{TagName}' (Id={Id})", tag.Name, tag.Id);
            return tag;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            _db.Entry(tag).State = EntityState.Detached;

            var loaded = await _db.Tags.FirstOrDefaultAsync(t => t.Name == normalized, ct);
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
    public async Task<IReadOnlyList<string>> SuggestAsync(string? q, int take = 20, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 50);
        var query = (q ?? "").Trim();

        var tags = _db.Tags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            // Postgres case-insensitive contains
            tags = tags.Where(t => EF.Functions.ILike(t.Name, $"%{query}%"));
        }

        return await tags
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .Take(take)
            .ToListAsync(ct);
    }

    private static string Normalize(string? input)
    {
        var s = (input ?? string.Empty).Trim();
        if (s.Length == 0) return s;
        s = Regex.Replace(s, @"\s+", " ");
        return s;
    }
}
