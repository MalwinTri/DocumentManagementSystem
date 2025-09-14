using DocumentManagementSystem.Domain;

namespace DocumentManagementSystem.Database.Repositories;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document doc, CancellationToken ct = default);
    Task<Document?> GetAsync(Guid id, CancellationToken ct = default);
    IQueryable<Document> Query();
}
