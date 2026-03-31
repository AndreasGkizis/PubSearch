namespace ResearchPublications.Application.DTOs;

public record AuthorManagementDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public int PublicationCount { get; init; }
}
