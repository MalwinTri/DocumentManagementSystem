using DocumentManagementSystem.Database;
using DocumentManagementSystem.Elasticsearch.Models;
using DocumentManagementSystem.Elasticsearch.Services;
using DocumentManagementSystem.Infrastructure.Exceptions;
using DocumentManagementSystem.Infrastructure.Services.GenAI;
using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DocumentManagementSystem.GenAI_Worker.AiWorker
{
    public class GenAiWorkerService : BackgroundService
    {
        private readonly ILogger<GenAiWorkerService> _logger;
        private readonly DmsDbContext _dbContext;
        private readonly IGenAiService _genAiService;
        private readonly ISearchIndexService _searchIndexService;

        // globaler Cooldown nach 429, damit du nicht andere Docs weiter spamst
        private DateTime? _pauseUntilUtc;

        public GenAiWorkerService(
            ILogger<GenAiWorkerService> logger,
            DmsDbContext dbContext,
            IGenAiService genAiService,
            ISearchIndexService searchIndexService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _genAiService = genAiService;
            _searchIndexService = searchIndexService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GenAI Worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                // global Pause nach RateLimit
                if (_pauseUntilUtc is DateTime pause && pause > now)
                {
                    await Task.Delay(pause - now, stoppingToken);
                    continue;
                }

                Document? doc = null;

                try
                {
                    doc = await _dbContext.Documents
                        .AsSplitQuery() // EF Warning "MultipleCollectionInclude" entschärfen
                        .Include(d => d.Tags)
                        .Include(d => d.Metadata)
                        .Include(d => d.ExtractedEntities)
                        .Include(d => d.Embedding)
                        .Where(d =>
                            d.OcrText != null &&
                            (d.Summary == null || d.Metadata == null || d.Embedding == null) &&
                            d.AiProcessedAt == null &&
                            (d.AiNextAttemptAt == null || d.AiNextAttemptAt <= now) &&
                            d.AiAttempts < 10
                        )
                        .OrderBy(d => d.CreatedAt)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (doc == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Processing document {DocumentId}", doc.Id);

                    //Lease/Reservation: damit es nicht sofort wieder gepickt wird 
                    doc.AiNextAttemptAt = now.AddMinutes(2);
                    doc.AiLastError = "IN_PROGRESS";
                    doc.UpdatedAt = now;
                    await _dbContext.SaveChangesAsync(stoppingToken);

                    // =========================================================
                    // 1) SUMMARY
                    // =========================================================
                    if (string.IsNullOrWhiteSpace(doc.Summary))
                    {
                        try
                        {
                            _logger.LogInformation("Generating summary for document {DocumentId}", doc.Id);

                            var summary = await _genAiService.GenerateSummaryAsync(doc.OcrText!, stoppingToken);

                            //  leeres Ergebnis = Failure -> Backoff!
                            if (string.IsNullOrWhiteSpace(summary))
                                throw new InvalidOperationException("Gemini returned empty summary");

                            doc.Summary = summary.Trim();
                            doc.UpdatedAt = DateTime.UtcNow;
                            doc.AiLastError = null;

                            await _dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Summary stored for document {DocumentId}", doc.Id);
                        }
                        catch (AiRateLimitException ex)
                        {
                            await StoreRateLimitBackoffAsync(doc, ex, stoppingToken);
                            _pauseUntilUtc = doc.AiNextAttemptAt; // ✅ global Pause
                            continue;
                        }
                        catch (Exception ex)
                        {
                            await StoreGenericBackoffAsync(doc, ex, stoppingToken);
                            continue;
                        }
                    }

                    // =========================================================
                    // 2) METADATA + ENTITIES + AUTO TAGGING
                    // =========================================================
                    if (doc.Metadata == null)
                    {
                        try
                        {
                            _logger.LogInformation("Extracting metadata/entities for document {DocumentId}", doc.Id);

                            var extraction = await _genAiService.ExtractMetadataAsync(doc.OcrText!, stoppingToken);

                            // null Ergebnis = Failure -> Backoff!
                            if (extraction == null)
                                throw new InvalidOperationException("Gemini returned null metadata extraction");

                            var meta = new DocumentMetadata
                            {
                                DocumentId = doc.Id,
                                DocumentType = string.IsNullOrWhiteSpace(extraction.DocumentType) ? "OTHER" : extraction.DocumentType.Trim(),
                                InvoiceNumber = NullIfEmpty(extraction.InvoiceNumber),
                                Iban = NullIfEmpty(extraction.Iban),
                                RawJson = string.IsNullOrWhiteSpace(extraction.RawJson) ? "{}" : extraction.RawJson
                            };

                            if (DateOnly.TryParse(extraction.IssueDate, out var issueDate))
                                meta.IssueDate = issueDate;

                            _dbContext.DocumentMetadatas.Add(meta);
                            doc.Metadata = meta;

                            // Entities ersetzen
                            if (doc.ExtractedEntities.Count > 0)
                                _dbContext.ExtractedEntities.RemoveRange(doc.ExtractedEntities);

                            foreach (var p in extraction.Persons.Distinct(StringComparer.OrdinalIgnoreCase))
                                _dbContext.ExtractedEntities.Add(new ExtractedEntity { DocumentId = doc.Id, Type = "PERSON", Value = p.Trim() });

                            foreach (var o in extraction.Organizations.Distinct(StringComparer.OrdinalIgnoreCase))
                                _dbContext.ExtractedEntities.Add(new ExtractedEntity { DocumentId = doc.Id, Type = "ORG", Value = o.Trim() });

                            foreach (var l in extraction.Locations.Distinct(StringComparer.OrdinalIgnoreCase))
                                _dbContext.ExtractedEntities.Add(new ExtractedEntity { DocumentId = doc.Id, Type = "LOCATION", Value = l.Trim() });

                            // Auto-Tags (MVP)
                            await AddTagIfMissingAsync(doc, $"type:{meta.DocumentType}", stoppingToken);

                            if (meta.IssueDate.HasValue)
                            {
                                var year = meta.IssueDate.Value.Year;
                                await AddTagIfMissingAsync(doc, $"year:{year}", stoppingToken);

                                var q = ((meta.IssueDate.Value.Month - 1) / 3) + 1;
                                await AddTagIfMissingAsync(doc, $"Q{q}-{year}", stoppingToken);
                            }

                            foreach (var org in extraction.Organizations.Take(5))
                                await AddTagIfMissingAsync(doc, $"org:{org}", stoppingToken);

                            foreach (var kw in extraction.Keywords.Take(10))
                                await AddTagIfMissingAsync(doc, $"kw:{kw}", stoppingToken);

                            doc.UpdatedAt = DateTime.UtcNow;
                            doc.AiLastError = null;

                            await _dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Metadata/entities/tags stored for document {DocumentId}", doc.Id);
                        }
                        catch (AiRateLimitException ex)
                        {
                            await StoreRateLimitBackoffAsync(doc, ex, stoppingToken);
                            _pauseUntilUtc = doc.AiNextAttemptAt;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            await StoreGenericBackoffAsync(doc, ex, stoppingToken);
                            continue;
                        }
                    }

                    // =========================================================
                    // 3) EMBEDDING
                    // =========================================================
                    if (doc.Embedding == null)
                    {
                        try
                        {
                            _logger.LogInformation("Generating embedding for document {DocumentId}", doc.Id);

                            var embedText = $"{doc.Title}\n\n{doc.Summary ?? ""}\n\n{doc.OcrText}";
                            var vector = await _genAiService.GenerateEmbeddingAsync(embedText, stoppingToken);

                            // leeres Ergebnis = Failure -> Backoff!
                            if (vector == null || vector.Length == 0)
                                throw new InvalidOperationException("Gemini returned empty embedding");

                            var emb = new DocumentEmbedding
                            {
                                DocumentId = doc.Id,
                                Model = "gemini-embedding-001",
                                Dims = vector.Length,
                                VectorJson = JsonSerializer.Serialize(vector)
                            };

                            _dbContext.DocumentEmbeddings.Add(emb);
                            doc.Embedding = emb;

                            doc.UpdatedAt = DateTime.UtcNow;
                            doc.AiLastError = null;

                            await _dbContext.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("Embedding stored for document {DocumentId} dims={Dims}", doc.Id, vector.Length);
                        }
                        catch (AiRateLimitException ex)
                        {
                            await StoreRateLimitBackoffAsync(doc, ex, stoppingToken);
                            _pauseUntilUtc = doc.AiNextAttemptAt;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            await StoreGenericBackoffAsync(doc, ex, stoppingToken);
                            continue;
                        }
                    }

                    // Wenn alles fertig ist -> AiProcessedAt setzen + Reservation löschen
                    if (doc.Summary != null && doc.Metadata != null && doc.Embedding != null && doc.AiProcessedAt == null)
                    {
                        doc.AiProcessedAt = DateTime.UtcNow;
                        doc.AiNextAttemptAt = null;
                        doc.AiLastError = null;
                        doc.UpdatedAt = DateTime.UtcNow;

                        await _dbContext.SaveChangesAsync(stoppingToken);
                    }

                    // =========================================================
                    // 4) Elasticsearch indexieren (nur einmal)
                    // =========================================================
                    if (doc.AiProcessedAt != null && doc.IndexedAt == null)
                    {
                        try
                        {
                            var indexDoc = doc.ToDocumentIndex();
                            await _searchIndexService.IndexDocumentAsync(indexDoc, stoppingToken);

                            doc.IndexedAt = DateTime.UtcNow;
                            doc.UpdatedAt = DateTime.UtcNow;
                            await _dbContext.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("Document {DocumentId} indexed in Elasticsearch", doc.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to index document {DocumentId} in Elasticsearch", doc.Id);
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GenAI worker loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("GenAI Worker stopping");
        }

        private async Task StoreRateLimitBackoffAsync(Document doc, AiRateLimitException ex, CancellationToken ct)
        {
            doc.AiAttempts += 1;
            doc.AiLastError = "429 RATE_LIMIT";

            var jitter = TimeSpan.FromSeconds(Random.Shared.Next(1, 5));
            doc.AiNextAttemptAt = DateTime.UtcNow.Add(ex.RetryAfter).Add(jitter);

            doc.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Rate limit for {DocumentId}. Next attempt at {NextAttempt} (attempt {Attempt}/10)",
                doc.Id, doc.AiNextAttemptAt, doc.AiAttempts);
        }

        private async Task StoreGenericBackoffAsync(Document doc, Exception ex, CancellationToken ct)
        {
            doc.AiAttempts += 1;
            doc.AiLastError = ex.Message.Length > 200 ? ex.Message[..200] : ex.Message;

            // kleiner Backoff (du kannst hier gern exponentiell machen)
            doc.AiNextAttemptAt = DateTime.UtcNow.AddMinutes(5);
            doc.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogWarning(
                ex,
                "AI error for {DocumentId}. Next attempt at {NextAttempt} (attempt {Attempt}/10)",
                doc.Id, doc.AiNextAttemptAt, doc.AiAttempts);
        }

        private async Task AddTagIfMissingAsync(Document doc, string name, CancellationToken ct)
        {
            name = Normalize(name);
            if (string.IsNullOrWhiteSpace(name)) return;

            if (name.Length > 64) name = name[..64];

            if (doc.Tags.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return;

            var lower = name.ToLowerInvariant();
            var existing = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Name == name, ct);

            if (existing == null)
            {
                existing = new Tag { Name = name };
                _dbContext.Tags.Add(existing);
                await _dbContext.SaveChangesAsync(ct);
            }

            doc.Tags.Add(existing);
        }

        private static string Normalize(string? s) =>
            string.Join(' ', (s ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        private static string? NullIfEmpty(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
