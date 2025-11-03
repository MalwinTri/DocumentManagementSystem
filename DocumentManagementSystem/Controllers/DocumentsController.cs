using DocumentManagementSystem.BL.Documents;
using DocumentManagementSystem.Dto;
using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Infrastructure.Exceptions;
using DocumentManagementSystem.Mapping;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _service;
    private readonly RabbitMqService _rabbitMqService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        DocumentService service,
        RabbitMqService rabbitMqService,
        ILogger<DocumentsController> logger)
    {
        _service = service;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Upload validation failed: {ModelState}", ModelState);
            return ValidationProblem(ModelState);
        }

        var errors = new Dictionary<string, string[]>();
        if (form.File is null || form.File.Length == 0) errors["file"] = new[] { "File is required." };
        if (string.IsNullOrWhiteSpace(form.Title)) errors["title"] = new[] { "Title is required." };
        if (errors.Count > 0) throw new ValidationException(errors: errors);

        _logger.LogInformation("Upload started for Title={Title}", form.Title);
        // Metadaten speichern
        await using var fileStream = form.File?.OpenReadStream();
        var saved = await _service.CreateAsync(form.Title, form.Description, form.Tags ?? new(), fileStream, ct);

        // 2) Datei ins SHARED-Volume speichern (/app/files)
        string fullPath;
        try
        {
            var root = Environment.GetEnvironmentVariable("STORAGE_ROOT") ?? "/app";
            var sub = Environment.GetEnvironmentVariable("STORAGE_DOCS_SUBFOLDER") ?? "files";
            var folder = Path.Combine(root, sub);              // -> /app/files
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(form.File.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";
            ext = ext.ToLowerInvariant();

            // wir benennen strikt nach Id + Original-Extension
            fullPath = Path.Combine(folder, $"{saved.Id}{ext}");

            await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await form.File.CopyToAsync(stream, ct);

            _logger.LogInformation("Saved file at {Path}", fullPath);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File IO error for DocumentId={DocumentId}", saved.Id);
            throw new FileStorageException("Could not persist uploaded file", "file_io_error", ex);
        }
        // 3) Nur PDFs in die OCR-Queue schicken (REST: alles akzeptiert)
        try
        {
            var isPdf = Path.GetExtension(fullPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(form.File.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

            if (isPdf)
            {
                // Models.OcrJob enthält DocumentId, LocalPath, ContentType, UploadedAt
                var job = new OcrJob(saved.Id, fullPath, "application/pdf", DateTime.UtcNow);
                _rabbitMqService.SendOcrMessage(job);
                _logger.LogInformation("OCR job queued for DocumentId={DocumentId}", saved.Id);
            }
            else
            {
                _logger.LogInformation("Non-PDF uploaded; skipping OCR for DocumentId={DocumentId}", saved.Id);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when sending OCR message for DocumentId={DocumentId}", saved.Id);
            throw new FileStorageException("Could not enqueue OCR job", code: "enqueue_failed", inner: ex);
        }

        _logger.LogInformation("Upload finished for DocumentId={DocumentId}", saved.Id);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, DocumentMapper.ToDto(saved));
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var d = await _service.GetAsync(id, ct);
        var doc = d ?? throw NotFoundException.For<Document>(id);
        return Ok(DocumentMapper.ToDto(doc));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var ok = await _service.DeleteAsync(id, ct);
        if (!ok) throw NotFoundException.For<Document>(id);
        return NoContent();
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids, CancellationToken ct)
    {
        // Strenger wie in File 1 (ValidationException statt BadRequest)
        if (ids is null || ids.Count == 0)
            throw new ValidationException(errors: new Dictionary<string, string[]>
            {
                ["ids"] = new[] { "At least one id is required." }
            });

        var deleted = await _service.DeleteManyAsync(ids, ct);
        return Ok(new { deleted });
    }

    [HttpGet] // GET /api/Documents?page=0&size=20
    public async Task<IActionResult> List([FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var (items, total) = await _service.ListAsync(page, size, ct);
        return Ok(new
        {
            items = items.Select(DocumentMapper.ToDto).ToList(),
            total,
            page,
            size
        });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] DocumentUpdateDto dto, CancellationToken ct)
    {
        if (dto is null) throw new ValidationException(detail: "Body is required");

        var updated = await _service.UpdateAsync(id, dto.Title, dto.Description, dto.Tags ?? new(), ct);

        var doc = updated ?? throw NotFoundException.For<Document>(id);

        return Ok(DocumentMapper.ToDto(doc));
    }


    // Optional als Alias:
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Put([FromRoute] Guid id, [FromBody] DocumentUpdateDto dto, CancellationToken ct)
        => Update(id, dto, ct);
}
