namespace ResearchPublications.Domain.Entities;

public class Publication : BaseDbEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string? Body { get; set; }
    public int? Year { get; set; }
    public string? DOI { get; set; }
    public string? PdfFileName { get; set; }

    public List<Author> Authors { get; set; } = [];
    public List<Keyword> Keywords { get; set; } = [];
    public List<Language> Languages { get; set; } = [];
    public List<PublicationType> PublicationTypes { get; set; } = [];
}
