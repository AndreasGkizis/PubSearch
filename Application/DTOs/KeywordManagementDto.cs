namespace ResearchPublications.Application.DTOs;

public class KeywordManagementDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int PublicationCount { get; set; }
}
