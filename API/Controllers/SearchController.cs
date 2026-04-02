using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(IServiceProvider serviceProvider) : ControllerBase
{
    private static readonly HashSet<string> ValidProviders = ["typesense", "mssql"];

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string?   q,
        [FromQuery] string    provider = "typesense",
        [FromQuery] int       page     = 1,
        [FromQuery] int       pageSize = 20,
        [FromQuery] int?      yearFrom = null,
        [FromQuery] int?      yearTo   = null,
        [FromQuery] string[]? authors  = null,
        [FromQuery] string[]? keywords = null,
        [FromQuery] string[]? languages = null,
        [FromQuery] string[]? publicationTypes = null)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required." });

        provider = provider.ToLowerInvariant();
        if (!ValidProviders.Contains(provider))
            return BadRequest(new { error = $"Invalid provider '{provider}'. Use 'typesense' or 'mssql'." });

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 20) pageSize = 20;

        var searchService = serviceProvider.GetRequiredKeyedService<ISearchService>(provider);
        var filters = new SearchFilters(yearFrom, yearTo, authors, keywords, languages, publicationTypes);

        var sw = Stopwatch.StartNew();
        var (items, total) = await searchService.SearchAsync(q, filters, page, pageSize);
        sw.Stop();

        return Ok(new { items, total, page, pageSize, provider, elapsedMs = sw.ElapsedMilliseconds });
    }
}
