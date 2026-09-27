namespace ResearchPublications.Application.DTOs;

public sealed record OcrSearchResultDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<string> Authors { get; init; } = [];
    public int? Year { get; init; }
    public string? OcrSnippet { get; init; }
    public string? PdfFileName { get; init; }
}
