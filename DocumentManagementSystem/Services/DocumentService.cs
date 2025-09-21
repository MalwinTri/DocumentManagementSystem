using DocumentManagementSystem.Domain;
using DocumentManagementSystem.Database.Repositories;

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

    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default)
        => _docRepo.GetAsync(id, ct);
}