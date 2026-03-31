using System.ComponentModel.DataAnnotations.Schema;

namespace ResearchPublications.Domain.Entities;

public class Author : BaseDbEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }

    public List<Publication> Publications { get; set; } = [];

    [NotMapped]
    public int PublicationCount { get; set; }
}
