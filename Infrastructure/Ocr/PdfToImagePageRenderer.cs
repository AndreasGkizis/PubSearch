using System.Runtime.CompilerServices;
using PDFtoImage;
using SkiaSharp;

namespace ResearchPublications.Infrastructure.Ocr;

internal sealed class PdfToImagePageRenderer : IOcrPageRenderer
{
    public async IAsyncEnumerable<byte[]> RenderPngPagesAsync(
        Stream pdfStream,
        int dpi,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var bitmap in Conversion.ToImagesAsync(
            pdfStream,
            leaveOpen: true,
            password: null,
            options: new RenderOptions(Dpi: dpi),
            cancellationToken: cancellationToken))
        {
            using (bitmap)
            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                yield return data.ToArray();
            }
        }
    }
}
