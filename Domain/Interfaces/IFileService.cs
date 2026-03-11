namespace ResearchPublications.Domain.Interfaces;

public interface IFileService
{
    Task<Stream?> GetPdfAsync(string fileName);
    bool Exists(string fileName);
    /// <summary>Saves a stream as a PDF and returns the stored file name.</summary>
    Task<string> SavePdfAsync(Stream content, string originalFileName);
}
