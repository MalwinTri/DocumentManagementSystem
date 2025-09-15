using DocumentManagementSystem.Dto;        // Form-DTO für den Upload
using DocumentManagementSystem.Mapping;    // Mapper -> DTO
using DocumentManagementSystem.Services;   // Fachlogik-Service
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")] // ergibt: /api/documents
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _service;

    public DocumentsController(DocumentService service)
    {
        _service = service; // DI: Service wird vom Container bereitgestellt
    }

    /// <summary>
    /// Dokument hochladen (Sprint 1: nur Metadaten speichern, Dateiablage folgt später).
    /// </summary>
    /// <remarks>
    /// Erwartet multipart/form-data mit Feldern: file (IFormFile), title (string),
    /// optional description (string), mehrfach tags (List&lt;string&gt;).
    /// </remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] DocumentUploadForm form,    // alle Form-Felder sauber in einem Modell
        CancellationToken ct)
    {
        // ModelState-Validierung (prüft [Required], [MaxLength] im Form-DTO)
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Datei wird in Sprint 2/3 gespeichert (MinIO/FS). Hier: nur Metadaten in DB.
        var saved = await _service.CreateAsync(form.Title, form.Description, form.Tags ?? new(), ct);

        // 201 Created + Location-Header auf GET /api/documents/{id}
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, DocumentMapper.ToDto(saved));
    }

    /// <summary>
    /// Dokumente listen (mit Paging).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 0,              // 0-basierte Seite
        [FromQuery] int size = 20,             // Elemente pro Seite (max. 100)
        CancellationToken ct = default)
    {
        // Eingabe absichern
        if (page < 0 || size <= 0 || size > 100)
            return BadRequest(new { error = "invalid paging parameters" });

        // Service liefert Items + Total in EINER DB-Query (Skip/Take)
        var (items, total) = await _service.ListAsync(page, size, ct);

        // Typische Paging-Struktur für Frontends
        return Ok(new
        {
            content = items.Select(DocumentMapper.ToDto).ToList(),
            page,
            size,
            totalElements = total
        });
    }

    /// <summary>
    /// Ein Dokument per Id abrufen.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var d = await _service.GetAsync(id, ct);
        return d is null ? NotFound() : Ok(DocumentMapper.ToDto(d));
    }
}
