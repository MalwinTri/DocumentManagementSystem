using DocumentManagementSystem.Models;
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

    public async Task<Document?> UpdateAsync(
     Guid id,
     string? title,
     string? description,
     List<string>? tags,
     CancellationToken ct = default)
    {
        var doc = await _docRepo.GetAsync(id, ct);
        if (doc is null) return null;

        if (!string.IsNullOrWhiteSpace(title)) doc.Title = title;
        if (description is not null) doc.Description = description;

        if (tags is not null)
        {
            doc.Tags.Clear();
            foreach (var raw in tags)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var tag = await _tagRepo.GetOrCreateAsync(raw, ct);
                doc.Tags.Add(tag);
            }
        }

        await _docRepo.SaveChangesAsync(ct);  
        return doc;
    }


    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default)
        => _docRepo.GetAsync(id, ct);

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => _docRepo.DeleteAsync(id, ct);

    public Task<int> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => _docRepo.DeleteManyAsync(ids, ct);

    public async Task<(IReadOnlyList<Document> Items, int Total)> ListAsync(int page, int size, CancellationToken ct = default)
    {
        var query = _docRepo.Query().OrderByDescending(d => d.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(page * size).Take(size).ToListAsync(ct);
        return (items, total);
    }
}