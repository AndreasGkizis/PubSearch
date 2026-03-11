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
        [FromQuery] string? q,
        [FromQuery] int?    year,
        [FromQuery] string? author,
        [FromQuery] string? keyword)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required." });

        var filters = new SearchFilters(year, author, keyword);
        var results = await searchService.SearchAsync(q, filters);
        return Ok(results);
    }
}
