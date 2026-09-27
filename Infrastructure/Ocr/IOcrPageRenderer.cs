namespace ResearchPublications.Infrastructure.Ocr;

internal interface IOcrPageRenderer
{
    IAsyncEnumerable<byte[]> RenderPngPagesAsync(
        Stream pdfStream,
        int dpi,
        CancellationToken cancellationToken = default);
}
