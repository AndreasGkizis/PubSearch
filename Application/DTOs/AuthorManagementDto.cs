namespace ResearchPublications.Application.DTOs;

public class AuthorManagementDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int PublicationCount { get; set; }
}
