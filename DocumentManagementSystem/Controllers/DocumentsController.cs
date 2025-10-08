using DocumentManagementSystem.Dto;
using DocumentManagementSystem.Mapping;
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

    public DocumentsController(DocumentService service, RabbitMqService rabbitMqService, ILogger<DocumentsController> logger)
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

        _logger.LogInformation("Upload started for Title={Title}", form.Title);

        var saved = await _service.CreateAsync(form.Title, form.Description, form.Tags ?? new(), ct);

        var safeTitle = string.Concat(form.Title.Where(c => char.IsLetterOrDigit(c) || c == '_'));
        var extension = Path.GetExtension(form.File.FileName);
        var fileName = $"{safeTitle}_{saved.Id}{extension}";
        var filePath = Path.Combine("files", fileName);
        Directory.CreateDirectory("files");
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await form.File.CopyToAsync(stream, ct);
        }

        try
        {
            _rabbitMqService.SendOcrMessage(new { DocumentId = saved.Id, FileName = fileName });
            _logger.LogInformation("OCR message enqueued for DocumentId={DocumentId}", saved.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when sending OCR message for DocumentId={DocumentId}", saved.Id);
        }

        _logger.LogInformation("Upload finished for DocumentId={DocumentId}", saved.Id);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, DocumentMapper.ToDto(saved));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var d = await _service.GetAsync(id, ct);
        return d is null ? NotFound() : Ok(DocumentMapper.ToDto(d));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var ok = await _service.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] List<Guid> ids, CancellationToken ct)
    {
        if (ids is null || ids.Count == 0)
        {
            _logger.LogWarning("BulkDelete called with empty or null ids");
            return BadRequest();
        }
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
        var updated = await _service.UpdateAsync(id, dto.Title, dto.Description, dto.Tags ?? new(), ct);
        return updated is null ? NotFound() : Ok(DocumentMapper.ToDto(updated));
    }

    // Optional alias:
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Put([FromRoute] Guid id, [FromBody] DocumentUpdateDto dto, CancellationToken ct)
        => Update(id, dto, ct);
}