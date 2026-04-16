namespace ResearchPublications.Application.Interfaces;

public interface IPdfTextExtractor
{
    /// <summary>Extracts all text from a PDF stream. Returns empty string if extraction fails.</summary>
    string Extract(Stream pdfStream);
}
