using ResearchPublications.Infrastructure.Settings;

namespace ResearchPublications.Infrastructure.Ocr;

internal sealed class OcrPdfProcessor(
    IOcrPageRenderer pageRenderer,
    IOllamaOcrClient ollama,
    OcrSettings settings) : IOcrPdfProcessor
{
    public async Task<string> ProcessAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        var pages = new List<string>();

        await foreach (var pngBytes in pageRenderer.RenderPngPagesAsync(
            pdfStream, settings.Dpi, cancellationToken))
        {
            pages.Add(await ollama.ExtractMarkdownAsync(pngBytes, cancellationToken));
        }

        if (pages.Count == 0)
            throw new InvalidOperationException("The PDF contains no renderable pages.");

        return string.Join("\n\n", pages);
    }
}
