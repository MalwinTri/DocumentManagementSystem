using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Database.Repositories;

public class DocumentRepository(DmsDbContext db) : IDocumentRepository
{
    public async Task<Document> AddAsync(Document doc, CancellationToken ct = default)
    {
        db.Documents.Add(doc);
        await db.SaveChangesAsync(ct);
        return doc;
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