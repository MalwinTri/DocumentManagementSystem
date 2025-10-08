using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentManagementSystem.Database.Repositories;

public class DocumentRepository(DmsDbContext db, ILogger<DocumentRepository> logger) : IDocumentRepository
{
    private readonly DmsDbContext _db = db;
    private readonly ILogger<DocumentRepository> _logger = logger;

    public async Task<Document> AddAsync(Document doc, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding document Title=\"{Title}\" to DB", doc.Title);

        _db.Documents.Add(doc);

        try
        {
            await SaveChangesSafeAsync(ct);
            _logger.LogInformation("Document saved to DB. DocumentId={DocumentId}", doc.Id);
            return doc;
        }
        catch (UniqueConstraintViolationException ex)
        {
            _logger.LogWarning(ex, "Unique constraint violation while adding document Title=\"{Title}\"", doc.Title);
            throw;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, "RepositoryException while adding document Title=\"{Title}\"", doc.Title);
            throw;
        }
    }

    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.Documents
           .Include(d => d.Tags)
           .FirstOrDefaultAsync(d => d.Id == id, ct);

    public IQueryable<Document> Query() =>
        _db.Documents
           .Include(d => d.Tags)
           .AsQueryable();

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Documents.FindAsync(new object?[] { id }, ct);
        if (entity is null)
        {
            _logger.LogWarning("DeleteAsync: document not found. DocumentId={DocumentId}", id);
            return false;
        }

        _db.Documents.Remove(entity);
        await SaveChangesSafeAsync(ct);
        _logger.LogInformation("Document deleted. DocumentId={DocumentId}", id);
        return true;
    }

    public async Task<int> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids?.ToList() ?? [];
        if (idList.Count == 0)
        {
            _logger.LogWarning("DeleteManyAsync called with empty id list");
            return 0;
        }

        var toDelete = await _db.Documents
                                .Where(d => idList.Contains(d.Id))
                                .ToListAsync(ct);

        if (toDelete.Count == 0)
        {
            _logger.LogWarning("DeleteManyAsync: no documents found for given ids");
            return 0;
        }

        _db.Documents.RemoveRange(toDelete);
        await SaveChangesSafeAsync(ct);

        _logger.LogInformation("DeleteManyAsync removed {Count} documents", toDelete.Count);
        return toDelete.Count;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => SaveChangesSafeAsync(ct);

    private async Task SaveChangesSafeAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new UniqueConstraintViolationException(
                constraintName: pg.ConstraintName,
                value: TryExtractConflictValue(pg.Detail),
                detail: pg.Detail,
                inner: ex
            );
        }
        catch (DbUpdateException ex)
        {
            throw new RepositoryException(
                message: "DB update failed",
                operation: "save_changes",
                entity: nameof(Document),
                inner: ex
            );
        }
    }

    private static object? TryExtractConflictValue(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return null;

        var openField = detail.IndexOf('(');
        var closeField = detail.IndexOf(')');
        if (openField < 0 || closeField <= openField) return null;
        var field = detail.Substring(openField + 1, closeField - openField - 1);

        var marker = ")=(";
        var openVal = detail.IndexOf(marker, StringComparison.Ordinal);
        if (openVal < 0) return new { field };

        openVal += marker.Length;
        var closeVal = detail.IndexOf(')', openVal);
        if (closeVal < 0) return new { field };

        var value = detail.Substring(openVal, closeVal - openVal);
        return new { field, value };
    }
}
