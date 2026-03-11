namespace ResearchPublications.Application.DTOs;

public class PublicationDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? Body { get; set; }
    public string? Keywords { get; set; }
    public int? Year { get; set; }
    public string? DOI { get; set; }
    public int CitationCount { get; set; }
    public string? PdfFileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public List<AuthorDto> Authors { get; set; } = [];
}

public class AuthorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
