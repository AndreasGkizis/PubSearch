namespace ResearchPublications.Infrastructure.Ocr;

internal interface IOllamaOcrClient
{
    Task<string> ExtractMarkdownAsync(byte[] pngBytes, CancellationToken cancellationToken = default);
}
