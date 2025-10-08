using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using DocumentManagementSystem.Exceptions;


namespace DocumentManagementSystem.Database.Repositories;

public class TagRepository(DmsDbContext db, ILogger<TagRepository> logger) : ITagRepository
{
    private readonly DmsDbContext _db = db;
    private readonly ILogger<TagRepository> _logger = logger;

    public async Task<Tag> GetOrCreateAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim();
        _logger.LogDebug("GetOrCreateAsync called for tag='{TagName}'", n);

        var existing = await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == n.ToLower(), ct);
        if (existing != null)
        {
            _logger.LogDebug("Tag exists: {TagName} (Id={Id})", existing.Name, existing.Id);
            return existing;
        }

        var tag = new Tag { Name = n };

        try
        {
            _db.Tags.Add(tag);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Created new tag '{TagName}' (Id={Id})", tag.Name, tag.Id);
            return tag;
        }

        catch (DbUpdateException dbx)
        {
            _logger.LogError(dbx, "DbUpdateException while creating tag '{TagName}'", n);

            if (dbx.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
                || dbx.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Detected unique constraint violation while creating tag '{TagName}'", n);
                throw new UniqueConstraintViolationException(inner: dbx);
            }

            throw new RepositoryException(inner: dbx);
        }
    }
}