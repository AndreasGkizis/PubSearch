namespace ResearchPublications.Application.DTOs;

public record PublicationDetailDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Abstract { get; init; }
    public string? Body { get; init; }
    public string? Keywords { get; init; }
    public string? Languages { get; init; }
    public string? PublicationTypes { get; init; }
    public int? Year { get; init; }
    public string? DOI { get; init; }
    public string? PdfFileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public List<AuthorDto> Authors { get; init; } = [];
}

public record AuthorDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }

    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}".Trim()
        : $"{FirstName} {MiddleName} {LastName}".Trim();
}
