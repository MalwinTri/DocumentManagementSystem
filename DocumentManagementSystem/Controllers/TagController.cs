using DocumentManagementSystem.DAL;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TagsController : ControllerBase
{
    private readonly ITagRepository _tags;

    public TagsController(ITagRepository tags) => _tags = tags;

    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest(
        [FromQuery] string? q,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var items = await _tags.SuggestAsync(q, take, ct);
        return Ok(items);
    }
}

