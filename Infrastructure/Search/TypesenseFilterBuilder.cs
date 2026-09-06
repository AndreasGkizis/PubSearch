using ResearchPublications.Application.DTOs;

namespace ResearchPublications.Infrastructure.Search;

internal static class TypesenseFilterBuilder
{
    public static string? Build(SearchFilters filters)
    {
        var parts = new List<string>();

        if (filters.YearFrom.HasValue)
            parts.Add($"year:>={filters.YearFrom.Value}");

        if (filters.YearTo.HasValue)
            parts.Add($"year:<={filters.YearTo.Value}");

        if (filters is { Authors.Count: > 0 })
            parts.Add($"authors:[{string.Join(",", filters.Authors.Select(EscapeValue))}]");

        if (filters is { Keywords.Count: > 0 })
            parts.Add($"keywords:[{string.Join(",", filters.Keywords.Select(EscapeValue))}]");

        if (filters is { Languages.Count: > 0 })
            parts.Add($"languages:[{string.Join(",", filters.Languages.Select(EscapeValue))}]");

        if (filters is { PublicationTypes.Count: > 0 })
            parts.Add($"publication_types:[{string.Join(",", filters.PublicationTypes.Select(EscapeValue))}]");

        return parts.Count > 0 ? string.Join(" && ", parts) : null;
    }

    private static string EscapeValue(string value)
    {
        var escaped = value.Replace("`", "\\`");
        return $"`{escaped}`";
    }
}
