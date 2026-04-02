namespace ResearchPublications.Application.DTOs;

public record SearchResultDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<string> Authors { get; init; } = [];
    public int? Year { get; init; }
    public string? Keywords { get; init; }
    public string? Languages { get; init; }
    public string? PublicationTypes { get; init; }
    public string? AbstractSnippet { get; init; }
    public string? HighlightedTitle { get; init; }
    public List<string>? HighlightedAuthors { get; init; }
    public string? HighlightedKeywords { get; init; }
    public string? PdfFileName { get; init; }
    public double Rank { get; init; }
}
