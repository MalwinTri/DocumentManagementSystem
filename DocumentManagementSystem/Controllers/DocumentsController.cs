using DocumentManagementSystem.Dto;
using DocumentManagementSystem.Mapping;
using DocumentManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _service;

    public DocumentsController(DocumentService service)
    {
        _service = service;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var saved = await _service.CreateAsync(form.Title, form.Description, form.Tags ?? new(), ct);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, DocumentMapper.ToDto(saved));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var d = await _service.GetAsync(id, ct);
        return d is null ? NotFound() : Ok(DocumentMapper.ToDto(d));
    }
}
