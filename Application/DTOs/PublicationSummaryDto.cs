namespace ResearchPublications.Application.DTOs;

public record PublicationSummaryDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<string> Authors { get; init; } = [];
    public int? Year { get; init; }
    public string? Keywords { get; init; }
    public string? AbstractSnippet { get; init; }
    public string? PdfFileName { get; init; }
}
