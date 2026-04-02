using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using Typesense;

namespace ResearchPublications.Infrastructure.Search;

public class TypesenseSearchService(ITypesenseClient typesense) : ISearchService
{
    private const string CollectionName = "publications";

    public async Task<(IEnumerable<SearchResultDto> Items, int TotalCount)> SearchAsync(
        string query, SearchFilters filters, int page, int pageSize)
    {
        var searchText = string.IsNullOrWhiteSpace(query) ? "*" : query;

        var searchParams = new SearchParameters(searchText, "title,abstract,keywords,authors,body")
        {
            QueryByWeights = "5,3,2,2,1",
            FilterBy = BuildFilterBy(filters),
            Page = page,
            PerPage = pageSize,
            HighlightFields = "title,abstract,body,authors,keywords",
            HighlightStartTag = "<mark>",
            HighlightEndTag = "</mark>",
            HighlightAffixNumberOfTokens = 12,
            NumberOfTypos = "2",
            ExhaustiveSearch = false,
        };

        var result = await typesense.Search<PublicationDocument>(CollectionName, searchParams);

        var items = (result.Hits ?? []).Select(hit =>
        {
            var doc = hit.Document;
            var highlights = hit.Highlights ?? [];

            var abstractHighlight = highlights.FirstOrDefault(h => h.Field == "abstract");
            var bodyHighlight = highlights.FirstOrDefault(h => h.Field == "body");
            var titleHighlight = highlights.FirstOrDefault(h => h.Field == "title");
            var authorsHighlight = highlights.FirstOrDefault(h => h.Field == "authors");
            var keywordsHighlight = highlights.FirstOrDefault(h => h.Field == "keywords");

            string? snippet = abstractHighlight?.Snippet
                ?? bodyHighlight?.Snippet
                ?? (doc.Abstract.Length > 200 ? doc.Abstract[..200] + "\u2026" : NullIfEmpty(doc.Abstract));

            // For array fields, Typesense returns Snippets (array) rather than Snippet (string)
            var highlightedAuthors = authorsHighlight?.Snippets?.Count > 0
                ? authorsHighlight.Snippets.ToList()
                : null;

            var highlightedKeywords = keywordsHighlight?.Snippets?.Count > 0
                ? string.Join(", ", keywordsHighlight.Snippets)
                : null;

            var score = 0.0;
            if (hit.TextMatchInfo?.Score is { } scoreStr && long.TryParse(scoreStr, out var parsedScore))
                score = parsedScore;
            else if (hit.TextMatch.HasValue)
                score = hit.TextMatch.Value;

            return new SearchResultDto
            {
                Id = int.Parse(doc.Id),
                Title = doc.Title,
                Authors = doc.Authors.ToList(),
                Year = doc.Year == 0 ? null : doc.Year,
                Keywords = doc.Keywords.Length > 0 ? string.Join(", ", doc.Keywords) : null,
                Languages = doc.Languages.Length > 0 ? string.Join(", ", doc.Languages) : null,
                PublicationTypes = doc.PublicationTypes.Length > 0 ? string.Join(", ", doc.PublicationTypes) : null,
                AbstractSnippet = snippet,
                HighlightedTitle = titleHighlight?.Snippet,
                HighlightedAuthors = highlightedAuthors,
                HighlightedKeywords = highlightedKeywords,
                PdfFileName = NullIfEmpty(doc.PdfFileName),
                Rank = score
            };
        }).ToList();

        return (items, result.Found);
    }

    private static string? BuildFilterBy(SearchFilters filters)
    {
        var parts = new List<string>();

        if (filters.YearFrom.HasValue)
            parts.Add($"year:>={filters.YearFrom.Value}");

        if (filters.YearTo.HasValue)
            parts.Add($"year:<={filters.YearTo.Value}");

        if (filters is { Authors.Count: > 0 })
            parts.Add($"authors:[{string.Join(",", filters.Authors.Select(EscapeFilterValue))}]");

        if (filters is { Keywords.Count: > 0 })
            parts.Add($"keywords:[{string.Join(",", filters.Keywords.Select(EscapeFilterValue))}]");

        if (filters is { Languages.Count: > 0 })
            parts.Add($"languages:[{string.Join(",", filters.Languages.Select(EscapeFilterValue))}]");

        if (filters is { PublicationTypes.Count: > 0 })
            parts.Add($"publication_types:[{string.Join(",", filters.PublicationTypes.Select(EscapeFilterValue))}]");

        return parts.Count > 0 ? string.Join(" && ", parts) : null;
    }

    private static string EscapeFilterValue(string value)
    {
        // Typesense filter values that contain commas or backticks need to be backtick-escaped
        var escaped = value.Replace("`", "\\`");
        return $"`{escaped}`";
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
