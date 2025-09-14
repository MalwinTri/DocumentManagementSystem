using DocumentManagementSystem.Domain;
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
}
