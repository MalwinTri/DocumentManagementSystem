using DocumentManagementSystem.Models;
using DocumentManagementSystem.Database.Repositories;

using DocumentManagementSystem.Exceptions;



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

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
            errors["Title"] = new[] { "Title must be at least 3 characters." };

        var cleanedTags = (tags ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToList();

        if (cleanedTags.Count > 10)
            errors["Tags"] = new[] { "No more than 10 tags allowed." };

        if (errors.Count > 0)
            throw new ValidationException(errors: errors);



        var doc = new Document
        {
            Title = title.Trim(),
            Description = description
        };

        if (cleanedTags.Count > 0)
        {
            foreach (var tagName in cleanedTags)
            {
                var tag = await _tagRepo.GetOrCreateAsync(tagName, ct);
                doc.Tags.Add(tag);
            }
        }

        try
        {
            return await _docRepo.AddAsync(doc, ct);
        }

        catch (UniqueConstraintViolationException uex)
        {
            // We attach the original as "inner" for server logs, but expose a safe message to clients.

            throw new ConflictException(
                message: "A document with these attributes already exists.",
                inner: uex
            );
        }

    }

    public async Task<Document> GetAsync(Guid id, CancellationToken ct = default)
    {
        // Ask the repository for the entity (includes Tags).
        var doc = await _docRepo.GetAsync(id, ct);

        // If not found, we throw NotFoundException instead of returning null.
        // Effect: Middleware turns this into a consistent HTTP 404 ProblemDetails.
        if (doc is null)
            throw new NotFoundException(resource: "Document", resourceId: id);

        return doc;
    }
}