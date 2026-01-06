using DocumentManagementSystem.DAL;
using DocumentManagementSystem.Dto;
using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Infrastructure.Services;
using DocumentManagementSystem.Mapping;
using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DocumentManagementSystem.BL.Documents;

public class DocumentService
{
    private readonly IDocumentRepository _docRepo;
    private readonly ITagRepository _tagRepo;
    private readonly ILogger<DocumentService> _logger;
    private readonly IRabbitMqService _mq;
    private readonly IGarageS3Service _garageS3;

    public DocumentService(
        IDocumentRepository docRepo,
        ITagRepository tagRepo,
        ILogger<DocumentService> logger,
        IRabbitMqService mq,
        IGarageS3Service garageS3)
    {
        _docRepo = docRepo;
        _tagRepo = tagRepo;
        _logger = logger;
        _mq = mq;
        _garageS3 = garageS3;
    }

    public async Task<Document> CreateAsync(
        string title,
        string? description,
        List<string>? tags,
        Stream? pdfStream,
        CancellationToken ct = default)
    {
        _logger.LogInformation("CreateAsync started. Title=\"{Title}\", IncomingTags={TagCount}", title, tags?.Count ?? 0);

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
            errors["Title"] = new[] { "Title must be at least 3 characters." };

        // Tags sauber machen: trim, spaces, maxlen 64, distinct (case-insensitive), max 10
        var cleanedTags = CleanTags(tags);

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

            if (pdfStream != null)
            {
                var key = $"{added.Id}.pdf";
                await _garageS3.UploadPdfAsync(key, pdfStream);
                _logger.LogInformation("PDF uploaded to Garage S3. Key={Key}", key);
            }

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
        string? summary,
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
            doc.Title = title.Trim();
        }

        if (description is not null)
        {
            _logger.LogDebug("UpdateAsync: updating Description for DocumentId={DocumentId}", id);
            doc.Description = description;
        }

        if (tags is not null)
        {
            var cleanedTags = CleanTags(tags);

            if (cleanedTags.Count > 10)
                throw new ValidationException(errors: new Dictionary<string, string[]>
                {
                    ["Tags"] = new[] { "No more than 10 tags allowed." }
                });

            _logger.LogDebug(
                "UpdateAsync: replacing tags for DocumentId={DocumentId}. IncomingCount={Count} CleanedCount={CleanedCount}",
                id, tags.Count, cleanedTags.Count);

            doc.Tags.Clear();

            foreach (var tagName in cleanedTags)
            {
                try
                {
                    var tag = await _tagRepo.GetOrCreateAsync(tagName, ct);
                    doc.Tags.Add(tag);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to get or create tag '{TagName}' while updating DocumentId={DocumentId}",
                        tagName, id);
                    throw;
                }
            }
        }

        if (summary is not null)
        {
            _logger.LogDebug("UpdateAsync: updating Summary for DocumentId={DocumentId}", id);
            doc.Summary = summary;
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

    public async Task<IReadOnlyList<Document>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids?
            .Distinct()
            .ToList() ?? new List<Guid>();

        if (idList.Count == 0)
        {
            _logger.LogInformation("GetByIdsAsync called with empty id list");
            return Array.Empty<Document>();
        }

        _logger.LogInformation("GetByIdsAsync requested for {Count} documents", idList.Count);

        var query = _docRepo.Query()
            .Where(d => idList.Contains(d.Id));

        var items = await query.ToListAsync(ct);

        _logger.LogInformation("GetByIdsAsync returned {Count} documents", items.Count);
        return items;
    }

    // =========================
    // Helpers
    // =========================

    private static List<string> CleanTags(List<string>? tags)
    {
        // - trim
        // - collapse spaces
        // - cut to 64
        // - distinct case-insensitive
        // - keep order as much as possible
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in tags ?? new List<string>())
        {
            var t = NormalizeTag(raw);
            if (string.IsNullOrWhiteSpace(t)) continue;

            if (t.Length > 64)
                t = t[..64];

            if (seen.Add(t))
                result.Add(t);
        }

        return result;
    }

    private static string NormalizeTag(string? s)
    {
        // trim + mehrfach spaces weg
        var t = (s ?? "").Trim();
        if (t.Length == 0) return t;

        // collapse whitespace to single spaces
        while (t.Contains("  "))
            t = t.Replace("  ", " ");

        return t;
    }

    public async Task<IReadOnlyList<SimilarDocumentResponseDto>> GetSimilarAsync(
    Guid id,
    int take = 6,
    CancellationToken ct = default)
    {
        var target = await _docRepo.GetAsync(id, ct);
        if (target?.Embedding is null || string.IsNullOrWhiteSpace(target.Embedding.VectorJson))
            return Array.Empty<SimilarDocumentResponseDto>();

        var targetVec = ParseVector(target.Embedding.VectorJson);
        if (targetVec.Length == 0)
            return Array.Empty<SimilarDocumentResponseDto>();

        var candidates = await _docRepo.Query()
            .Where(d => d.Id != id && d.Embedding != null)
            .Select(d => new { Doc = d, VecJson = d.Embedding!.VectorJson })
            .ToListAsync(ct);

        var scored = new List<SimilarDocumentResponseDto>();

        foreach (var c in candidates)
        {
            var vec = ParseVector(c.VecJson);
            if (vec.Length != targetVec.Length || vec.Length == 0) continue;

            var score = CosineSimilarity(targetVec, vec);
            if (score <= 0) continue;

            scored.Add(new SimilarDocumentResponseDto
            {
                Document = DocumentMapper.ToDto(c.Doc),
                Score = score
            });
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(Math.Clamp(take, 1, 50))
            .ToList();
    }

    private static float[] ParseVector(string json)
    {
        try { return JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>(); }
        catch { return Array.Empty<float>(); }
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;

        for (int i = 0; i < a.Length; i++)
        {
            var x = a[i];
            var y = b[i];
            dot += x * y;
            na += x * x;
            nb += y * y;
        }

        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        if (denom <= 1e-12) return 0;
        return dot / denom;
    }

}
