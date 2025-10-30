using DocumentManagementSystem.Services;
using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.DAL;

namespace DocumentManagementSystem.BL.Documents;

public class DocumentService
{
    private readonly IDocumentRepository _docRepo;
    private readonly ITagRepository _tagRepo;
    private readonly ILogger<DocumentService> _logger;
    private readonly RabbitMqService _mq;

    public DocumentService(
        IDocumentRepository docRepo,
        ITagRepository tagRepo,
        ILogger<DocumentService> logger,
        RabbitMqService mq)
    {
        _docRepo = docRepo;
        _tagRepo = tagRepo;
        _logger = logger;
        _mq = mq;
    }

    public async Task<Document> CreateAsync(
        string title,
        string? description,
        List<string>? tags,
        CancellationToken ct = default)
    {
        _logger.LogInformation("CreateAsync started. Title=\"{Title}\", IncomingTags={TagCount}", title, tags?.Count ?? 0);

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
        {
            _logger.LogWarning("CreateAsync validation failed for Title=\"{Title}\". Errors={Errors}", title, errors);
            throw new ValidationException(errors: errors);
        }

        var doc = new Document
        {
            Title = title.Trim(),
            Description = description
        };

        if (cleanedTags.Count > 0)
        {
            foreach (var tagName in cleanedTags)
            {
                try
                {
                    _logger.LogDebug("Resolving tag '{TagName}'", tagName);
                    var tag = await _tagRepo.GetOrCreateAsync(tagName, ct);
                    doc.Tags.Add(tag);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get or create tag '{TagName}'", tagName);
                    throw;
                }
            }
        }

        try
        {
            var added = await _docRepo.AddAsync(doc, ct);
            _logger.LogInformation("Document created successfully. DocumentId={DocumentId}", added.Id);

            try
            {
                var payload = new
                {
                    documentId = added.Id,
                    title = added.Title,
                    uploadedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Enqueuing OCR message for DocumentId={DocumentId}", added.Id);
                _mq.SendOcrMessage(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue OCR message for DocumentId={DocumentId}", added.Id);
            }

            return added;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add Document Title=\"{Title}\" to repository", title);
            throw;
        }
    }

    public async Task<Document?> UpdateAsync(
        Guid id,
        string? title,
        string? description,
        List<string>? tags,
        CancellationToken ct = default)
    {
        _logger.LogInformation("UpdateAsync started for DocumentId={DocumentId}", id);

        var doc = await _docRepo.GetAsync(id, ct);
        if (doc is null)
        {
            _logger.LogWarning("UpdateAsync: document not found. DocumentId={DocumentId}", id);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            _logger.LogDebug("UpdateAsync: updating Title for DocumentId={DocumentId}", id);
            doc.Title = title;
        }

        if (description is not null)
        {
            _logger.LogDebug("UpdateAsync: updating Description for DocumentId={DocumentId}", id);
            doc.Description = description;
        }

        if (tags is not null)
        {
            _logger.LogDebug("UpdateAsync: replacing tags for DocumentId={DocumentId}. IncomingCount={Count}", id, tags.Count);
            doc.Tags.Clear();
            foreach (var raw in tags)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                try
                {
                    var tag = await _tagRepo.GetOrCreateAsync(raw, ct);
                    doc.Tags.Add(tag);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get or create tag '{TagName}' while updating DocumentId={DocumentId}", raw, id);
                    throw;
                }
            }
        }

        try
        {
            await _docRepo.SaveChangesAsync(ct);
            _logger.LogInformation("UpdateAsync finished for DocumentId={DocumentId}", id);
            return doc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAsync failed saving changes for DocumentId={DocumentId}", id);
            throw;
        }
    }

    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("GetAsync requested for DocumentId={DocumentId}", id);
        return _docRepo.GetAsync(id, ct);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("DeleteAsync requested for DocumentId={DocumentId}", id);
        return _docRepo.DeleteAsync(id, ct);
    }

    public Task<int> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("DeleteManyAsync requested for {Count} documents", ids?.Count() ?? 0);
        return _docRepo.DeleteManyAsync(ids, ct);
    }

    public async Task<(IReadOnlyList<Document> Items, int Total)> ListAsync(int page, int size, CancellationToken ct = default)
    {
        _logger.LogDebug("ListAsync page={Page} size={Size}", page, size);
        var query = _docRepo.Query().OrderByDescending(d => d.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(page * size).Take(size).ToListAsync(ct);
        _logger.LogInformation("ListAsync returned {Returned} of {Total} total items for page={Page}", items.Count, total, page);
        return (items, total);
    }
}
