using Microsoft.EntityFrameworkCore;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Persistence;

namespace ResearchPublications.Infrastructure.Search;

public class MssqlSearchService(AppDbCntx context) : ISearchService
{
    public async Task<IEnumerable<SearchResultDto>> SearchAsync(string query, SearchFilters filters)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var q = context.Publications
            .Include(p => p.Authors)
            .Include(p => p.Keywords)
            .Where(p =>
                EF.Functions.FreeText(p.Title, query) ||
                EF.Functions.FreeText(p.Abstract!, query) ||
                EF.Functions.FreeText(p.Body!, query));

        if (filters.YearFrom.HasValue)
            q = q.Where(p => p.Year >= filters.YearFrom);

        if (filters.YearTo.HasValue)
            q = q.Where(p => p.Year <= filters.YearTo);

        if (filters.Authors is { Count: > 0 })
            q = q.Where(p => p.Authors.Any(a => filters.Authors.Contains(a.FullName)));

        if (filters.Keywords is { Count: > 0 })
            q = q.Where(p => p.Keywords.Any(k => filters.Keywords.Contains(k.Value)));

        var results = await q.ToListAsync();

        return results.Select(p => new SearchResultDto
        {
            Id = p.Id,
            Title = p.Title,
            Authors = p.Authors.Select(a => a.FullName).ToList(),
            Year = p.Year,
            Keywords = p.Keywords.Count > 0
                ? string.Join(", ", p.Keywords.Select(k => k.Value))
                : null,
            AbstractSnippet = p.Abstract is { Length: > 200 }
                ? p.Abstract[..200] + "…"
                : p.Abstract,
            PdfFileName = p.PdfFileName,
            Rank = 1.0
        });
    }
}
