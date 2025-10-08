using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentManagementSystem.Database.Repositories;

public class DocumentRepository(DmsDbContext db) : IDocumentRepository
{
    public async Task<Document> AddAsync(Document doc, CancellationToken ct = default)
    {
        db.Documents.Add(doc);
        await SaveChangesSafeAsync(ct);
        return doc;
    }

    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Documents.Include(d => d.Tags).FirstOrDefaultAsync(d => d.Id == id, ct);

    public IQueryable<Document> Query() =>
        db.Documents.Include(d => d.Tags).AsQueryable();

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Documents.FindAsync([id], ct);
        if (entity is null) return false;

        db.Documents.Remove(entity);
        await SaveChangesSafeAsync(ct);
        return true;
    }

    public async Task<int> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var toDelete = await db.Documents.Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        if (toDelete.Count == 0) return 0;

        db.Documents.RemoveRange(toDelete);
        await SaveChangesSafeAsync(ct);
        return toDelete.Count;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => SaveChangesSafeAsync(ct);

    private async Task SaveChangesSafeAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        // Postgres: 23505 = unique_violation
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

        var openVal = detail.IndexOf(")=(", StringComparison.Ordinal);
        if (openVal < 0) return new { field };

        openVal += 3;
        var closeVal = detail.IndexOf(')', openVal);
        if (closeVal < 0) return new { field };

        var value = detail.Substring(openVal, closeVal - openVal);
        return new { field, value };
    }
}
