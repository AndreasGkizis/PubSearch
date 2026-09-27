using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.Interfaces;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/ocr-search")]
public sealed class OcrSearchController(IOcrSearchService searchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required." });

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 20) pageSize = 20;

        var stopwatch = Stopwatch.StartNew();
        var (items, total) = await searchService.SearchAsync(
            q.Trim(), page, pageSize, cancellationToken);
        stopwatch.Stop();

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            elapsedMs = stopwatch.ElapsedMilliseconds
        });
    }
}
