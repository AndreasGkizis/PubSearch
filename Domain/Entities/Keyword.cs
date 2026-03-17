namespace ResearchPublications.Domain.Entities;

public class Keyword : BaseDbEntity
{
    public string Value { get; set; } = string.Empty;
    public List<Publication> Publications { get; set; } = [];
}