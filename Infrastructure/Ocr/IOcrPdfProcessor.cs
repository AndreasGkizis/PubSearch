namespace ResearchPublications.Infrastructure.Ocr;

internal interface IOcrPdfProcessor
{
    Task<string> ProcessAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default,
        Action<int>? pageStarted = null);
}
