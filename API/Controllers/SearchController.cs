using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string?   q,
        [FromQuery] int       page     = 1,
        [FromQuery] int       pageSize = 20,
        [FromQuery] int?      yearFrom = null,
        [FromQuery] int?      yearTo   = null,
        [FromQuery] string[]? authors  = null,
        [FromQuery] string[]? keywords = null)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required." });

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 20) pageSize = 20;

        var filters = new SearchFilters(yearFrom, yearTo, authors, keywords);
        var (items, total) = await searchService.SearchAsync(q, filters, page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }
}
