using Dapper;
using Microsoft.Data.SqlClient;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Constants;
using ResearchPublications.Infrastructure.Persistence;
using System.Data;

namespace ResearchPublications.Infrastructure.Search;

public class MssqlSearchService(DapperContext context) : ISearchService
{
    public async Task<IEnumerable<SearchResultDto>> SearchAsync(string query, SearchFilters filters)
    {
        using var conn = context.CreateConnection();
        var rows = await conn.QueryAsync<SearchRow>(
            StoredProcedures.SearchPublications,
            new
            {
                Query    = query,
                YearFrom = filters.YearFrom,
                YearTo   = filters.YearTo,
                Authors  = filters.Authors is { Count: > 0 } ? string.Join(",", filters.Authors) : null,
                Keywords = filters.Keywords is { Count: > 0 } ? string.Join(",", filters.Keywords) : null
            },
            commandType: CommandType.StoredProcedure);

        return rows.Select(r => new SearchResultDto
        {
            Id = r.Id,
            Title = r.Title,
            Authors = string.IsNullOrWhiteSpace(r.AuthorNames)
                ? []
                : r.AuthorNames.Split(", ").ToList(),
            Year = r.Year,
            Keywords = r.Keywords,
            AbstractSnippet = r.Abstract is { Length: > 200 }
                ? r.Abstract[..200] + "…"
                : r.Abstract,
            PdfFileName = r.PdfFileName,
            Rank = r.Rank
        });
    }

    private sealed class SearchRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Abstract { get; init; }
        public string? Keywords { get; init; }
        public int? Year { get; init; }
        public string? PdfFileName { get; init; }
        public double Rank { get; init; }
        public string? AuthorNames { get; init; }
    }
}
