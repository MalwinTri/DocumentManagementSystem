using DocumentManagementSystem.BL.Documents;
using DocumentManagementSystem.Dto;
using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Infrastructure.Services;
using DocumentManagementSystem.Mapping;
using DocumentManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _service;
    private readonly IRabbitMqService _rabbitMqService;   
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        DocumentService service,
        IRabbitMqService rabbitMqService,                 
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

        await using var fileStream = form.File!.OpenReadStream();
        var saved = await _service.CreateAsync(form.Title, form.Description, form.Tags ?? new(), fileStream, ct);

        var isPdf = string.Equals(form.File.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetExtension(form.File.FileName), ".pdf", StringComparison.OrdinalIgnoreCase);

        if (isPdf)
        {
            var s3Key = $"{saved.Id}.pdf";

            var job = new OcrJob
            {
                DocumentId = saved.Id,
                S3Key = s3Key,
                ContentType = "application/pdf",
                UploadedAt = DateTime.UtcNow
            };

            _rabbitMqService.SendOcrMessage(job);      
            _logger.LogInformation("OCR job queued for DocumentId={DocumentId} with S3Key={S3Key}", saved.Id, s3Key);
        }
        else
        {
            _logger.LogInformation("Non-PDF uploaded; skipping OCR for DocumentId={DocumentId}", saved.Id);
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
        if (ids is null || ids.Count == 0)
            throw new ValidationException(errors: new Dictionary<string, string[]>
            {
                ["ids"] = new[] { "At least one id is required." }
            });

        var deleted = await _service.DeleteManyAsync(ids, ct);
        return Ok(new { deleted });
    }

    [HttpGet]
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

        var updated = await _service.UpdateAsync(
            id,
            dto.Title,
            dto.Description,
            dto.Tags,      
            dto.Summary,    
            ct);

        var doc = updated ?? throw NotFoundException.For<Document>(id);

        return Ok(DocumentMapper.ToDto(doc));
    }


    [HttpPut("{id:guid}")]
    public Task<IActionResult> Put([FromRoute] Guid id, [FromBody] DocumentUpdateDto dto, CancellationToken ct)
    => Update(id, dto, ct);
}