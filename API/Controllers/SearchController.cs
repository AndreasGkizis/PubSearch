using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Search;
using Typesense;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(IServiceProvider serviceProvider, ITypesenseClient typesense) : ControllerBase
{
    private static readonly HashSet<string> ValidProviders = ["typesense", "mssql"];
    private const string CollectionName = "publications";
    private static readonly HashSet<string> ValidFacetFields = ["authors", "keywords", "languages", "publication_types"];

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

    [HttpGet("facets")]
    public async Task<IActionResult> FacetSearch(
        [FromQuery] string field,
        [FromQuery] string q = "")
    {
        if (string.IsNullOrWhiteSpace(field) || !ValidFacetFields.Contains(field))
            return BadRequest(new { error = $"Invalid field. Use one of: {string.Join(", ", ValidFacetFields)}" });

        var searchParams = new SearchParameters("*", "title")
        {
            FacetBy = field,
            FacetQuery = $"{field}:{q}",
            MaxFacetValues = 50,
            PerPage = 0,
        };

        var result = await typesense.Search<PublicationDocument>(CollectionName, searchParams);

        var facetCounts = result.FacetCounts?
            .FirstOrDefault(f => f.FieldName == field)?
            .Counts ?? [];

        return Ok(facetCounts.Select(c => new
        {
            name = c.Value,
            count = c.Count,
            highlighted = c.Highlighted
        }));
    }
}
