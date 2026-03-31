using System.ComponentModel.DataAnnotations.Schema;

namespace ResearchPublications.Domain.Entities;

public class Language : BaseDbEntity
{
    public string Value { get; set; } = string.Empty;
    public List<Publication> Publications { get; set; } = [];

    [NotMapped]
    public int PublicationCount { get; set; }
}
