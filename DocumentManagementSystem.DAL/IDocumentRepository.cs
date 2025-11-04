using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.DAL;

public interface IDocumentRepository
{
    Task<Document?> GetAsync(Guid id, CancellationToken ct = default);
    IQueryable<Document> Query();
    Task<Document> AddAsync(Document doc, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default); 
}
