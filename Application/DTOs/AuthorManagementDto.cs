namespace ResearchPublications.Application.DTOs;

public record AuthorManagementDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public int PublicationCount { get; init; }
}
