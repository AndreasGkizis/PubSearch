namespace ResearchPublications.Application.DTOs;

public record SearchFilters(
    int? YearFrom,
    int? YearTo,
    IReadOnlyList<string>? Authors,
    IReadOnlyList<string>? Keywords);
