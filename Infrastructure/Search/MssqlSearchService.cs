using Microsoft.EntityFrameworkCore;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Persistence;

namespace ResearchPublications.Infrastructure.Search;

public class MssqlSearchService(AppDbCntx context) : ISearchService
{
    public async Task<(IEnumerable<SearchResultDto> Items, int TotalCount)> SearchAsync(
        string query, SearchFilters filters, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ([], 0);

        var q = context.Publications
            .AsNoTracking()
            .Where(p => p.Title.Contains(query));

        if (filters.YearFrom.HasValue)
            q = q.Where(p => p.Year >= filters.YearFrom);

        if (filters.YearTo.HasValue)
            q = q.Where(p => p.Year <= filters.YearTo);

        if (filters.Authors is { Count: > 0 })
            q = q.Where(p => p.Authors.Any(a =>
                filters.Authors.Contains(a.FirstName + (a.MiddleName != null ? " " + a.MiddleName : "") + " " + a.LastName)));

        if (filters.Keywords is { Count: > 0 })
            q = q.Where(p => p.Keywords.Any(k => filters.Keywords.Contains(k.Value)));

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.LastModified)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new SearchResultDto
            {
                Id = p.Id,
                Title = p.Title,
                Authors = p.Authors.Select(a =>
                    a.FirstName + (a.MiddleName != null ? " " + a.MiddleName : "") + " " + a.LastName
                ).ToList(),
                Year = p.Year,
                Keywords = p.Keywords.Any()
                    ? string.Join(", ", p.Keywords.Select(k => k.Value))
                    : null,
                AbstractSnippet = p.Abstract != null && p.Abstract.Length > 200
                    ? p.Abstract.Substring(0, 200) + "…"
                    : p.Abstract,
                PdfFileName = p.PdfFileName,
                Rank = 1.0
            })
            .ToListAsync();

        IEnumerable<SearchResultDto> enumerable = items;

        return (enumerable, total);
    }
}
