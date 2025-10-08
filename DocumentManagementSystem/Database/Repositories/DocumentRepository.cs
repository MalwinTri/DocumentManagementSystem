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
}
