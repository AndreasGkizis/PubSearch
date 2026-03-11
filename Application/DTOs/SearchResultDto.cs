namespace ResearchPublications.Application.DTOs;

public class SearchResultDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<string> Authors { get; set; } = [];
    public int? Year { get; set; }
    public string? Keywords { get; set; }
    public string? AbstractSnippet { get; set; }
    public string? PdfFileName { get; set; }
    public double Rank { get; set; }
}
