using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

using DocumentManagementSystem.Exceptions;


namespace DocumentManagementSystem.Database.Repositories;

public class DocumentRepository(DmsDbContext db) : IDocumentRepository
{
    public async Task<Document> AddAsync(Document doc, CancellationToken ct = default)
    {

        try
        {
            db.Documents.Add(doc);
            await db.SaveChangesAsync(ct);
            return doc;
        }

        catch (DbUpdateException dbx)
        {
            // We try to detect a uniqueness violation. This "Contains('unique')" check
            // is a simple starter so you can learn and test the flow immediately.
            // Effect: we throw a typed UniqueConstraintViolationException, which the service
            // can translate into a business ConflictException (HTTP 409).
            if (dbx.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
                || dbx.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
            {
                throw new UniqueConstraintViolationException(inner: dbx);
            }

            // Anything else from EF becomes a generic RepositoryException.
            // Effect: Middleware maps this to HTTP 500 (“Data access error”).
            throw new RepositoryException(inner: dbx);
        }

    }

    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Documents.Include(d => d.Tags).FirstOrDefaultAsync(d => d.Id == id, ct);

    public IQueryable<Document> Query() => db.Documents.Include(d => d.Tags).AsQueryable();

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Documents.FindAsync([id], ct);
        if (entity is null) return false;
        db.Documents.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var toDelete = await db.Documents.Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        if (toDelete.Count == 0) return 0;
        db.Documents.RemoveRange(toDelete);
        return await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
       => db.SaveChangesAsync(ct); 
}