using DocumentManagementSystem.Domain;
using DocumentManagementSystem.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Services;

public class DocumentService
{
    private readonly IDocumentRepository _docRepo;
    private readonly ITagRepository _tagRepo;

    public DocumentService(IDocumentRepository docRepo, ITagRepository tagRepo)
    {
        _docRepo = docRepo;
        _tagRepo = tagRepo;
    }

    // Dokument erstellen
    public async Task<Document> CreateAsync(
        string title,
        string? description,
        List<string>? tags,
        CancellationToken ct = default)
    {
        var doc = new Document
        {
            Title = title,
            Description = description
        };

        if (tags is { Count: > 0 })
        {
            foreach (var raw in tags)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var tag = await _tagRepo.GetOrCreateAsync(raw, ct);
                doc.Tags.Add(tag);
            }
        }

        return await _docRepo.AddAsync(doc, ct);
    }

    // Dokumente auflisten (mit Paging)
    public async Task<(IReadOnlyList<Document> Items, int Total)> ListAsync(
        int page,
        int size,
        CancellationToken ct = default)
    {
        var q = _docRepo.Query().OrderByDescending(x => x.CreatedAt);
        var total = await q.CountAsync(ct);
        var items = await q.Skip(page * size).Take(size).ToListAsync(ct);
        return (items, total);
    }

    // Einzelnes Dokument holen
    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default)
        => _docRepo.GetAsync(id, ct);
}
