namespace ResearchPublications.Infrastructure.Ocr;

internal interface IOcrPdfProcessor
{
    Task<string> ProcessAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
