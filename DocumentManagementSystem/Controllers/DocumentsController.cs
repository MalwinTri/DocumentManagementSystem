using DocumentManagementSystem.Dto;
using DocumentManagementSystem.Exceptions;
using DocumentManagementSystem.Mapping;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _service;
    private readonly RabbitMqService _rabbitMqService;

    public DocumentsController(DocumentService service, RabbitMqService rabbitMqService)
    {
        _service = service;
        _rabbitMqService = rabbitMqService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadForm form, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (form.File is null || form.File.Length == 0) errors["file"] = new[] { "File is required." };
        if (string.IsNullOrWhiteSpace(form.Title)) errors["title"] = new[] { "Title is required." };
        if (errors.Count > 0) throw new ValidationException(errors: errors);

        var saved = await _service.CreateAsync(form.Title, form.Description, form.Tags ?? new(), ct);

        try
        {
            var safeTitle = string.Concat(form.Title.Where(c => char.IsLetterOrDigit(c) || c == '_'));
            var file = form.File!;
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{safeTitle}_{saved.Id}{extension}";
            var dir = "files";
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new OperationFailedException("Could not persist uploaded file", code: "file_io_error", inner: ex);
        }

        try
        {
            _rabbitMqService.SendOcrMessage(new { DocumentId = saved.Id, FileName = saved.Title });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new OperationFailedException("Could not enqueue OCR job", code: "enqueue_failed", inner: ex);
        }

        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, DocumentMapper.ToDto(saved));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var d = await _service.GetAsync(id, ct);
        if (d is null) throw NotFoundException.For<Document>(id);
        return Ok(DocumentMapper.ToDto(d));
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

        var updated = await _service.UpdateAsync(id, dto.Title, dto.Description, dto.Tags ?? new(), ct);
        if (updated is null) throw NotFoundException.For<Document>(id);
        return Ok(DocumentMapper.ToDto(updated));
    }

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Put([FromRoute] Guid id, [FromBody] DocumentUpdateDto dto, CancellationToken ct)
        => Update(id, dto, ct);
}
