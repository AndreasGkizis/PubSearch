namespace ResearchPublications.Application.DTOs;

public record PublicationTypeManagementDto
{
    public int Id { get; init; }
    public string Value { get; init; } = string.Empty;
    public int PublicationCount { get; init; }
}
