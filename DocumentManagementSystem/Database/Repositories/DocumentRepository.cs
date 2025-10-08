using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using DocumentManagementSystem.Exceptions;


namespace DocumentManagementSystem.Database.Repositories;

public class DocumentRepository(DmsDbContext db, ILogger<DocumentRepository> logger) : IDocumentRepository
{
    private readonly DmsDbContext _db = db;
    private readonly ILogger<DocumentRepository> _logger = logger;

    public async Task<Document> AddAsync(Document doc, CancellationToken ct = default)
    {
        _logger.LogDebug("Adding document Title=\"{Title}\" to DB", doc.Title);

        try
        {
            _db.Documents.Add(doc);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Document saved to DB. DocumentId={DocumentId}", doc.Id);
            return doc;
        }

        catch (DbUpdateException dbx)
        {
            _logger.LogError(dbx, "DbUpdateException while adding document Title=\"{Title}\"", doc.Title);

            if (dbx.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
                || dbx.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Detected unique constraint violation while adding document");
                throw new UniqueConstraintViolationException(inner: dbx);
            }

            throw new RepositoryException(inner: dbx);
        }

    }

    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.Documents.Include(d => d.Tags).FirstOrDefaultAsync(d => d.Id == id, ct);

    public IQueryable<Document> Query() => _db.Documents.Include(d => d.Tags).AsQueryable();

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Documents.FindAsync(id, ct);
        if (entity is null)
        {
            _logger.LogWarning("DeleteAsync: document not found. DocumentId={DocumentId}", id);
            return false;
        }
        _db.Documents.Remove(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Document deleted. DocumentId={DocumentId}", id);
        return true;
    }

    public async Task<int> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var toDelete = await _db.Documents.Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        if (toDelete.Count == 0)
        {
            _logger.LogWarning("DeleteManyAsync: no documents found for given ids");
            return 0;
        }
        _db.Documents.RemoveRange(toDelete);
        var deleted = await _db.SaveChangesAsync(ct);
        _logger.LogInformation("DeleteManyAsync removed {Count} documents", deleted);
        return deleted;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
       => _db.SaveChangesAsync(ct);
}