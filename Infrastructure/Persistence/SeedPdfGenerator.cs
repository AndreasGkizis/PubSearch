using SkiaSharp;

namespace ResearchPublications.Infrastructure.Persistence;

internal sealed record SeedPdf(string Body, byte[] PdfBytes);

internal static class SeedPdfGenerator
{
    private const int Dpi = 200;
    private const int PageWidth = 1700;
    private const int PageHeight = 11 * Dpi;
    private const float PdfWidth = 612;
    private const float PdfHeight = 792;
    private const string FontResourceName =
        "ResearchPublications.Infrastructure.Assets.LiberationSans-Regular.ttf";

    private static readonly string[] BodySentences =
    [
        "Condition mapping recorded loose tesserae along the northern edge of the excavated pavement.",
        "Raking light revealed shallow mortar gaps that were not visible during the initial site survey.",
        "The treatment team applied a compatible lime grout beneath unstable areas using low-pressure syringes.",
        "Moisture readings were collected before intervention and repeated after the repaired bedding layer had cured.",
        "High-resolution photographs linked each observed crack to a numbered square on the conservation grid.",
        "Small glass tesserae retained traces of blue and green color beneath a thin layer of mineral accretion.",
        "Temporary edging protected the exposed border while visitors were redirected around the working area.",
        "Salt deposits were removed mechanically under magnification without abrading the original stone surfaces.",
        "The final inspection found improved contact between the pavement and its supporting mortar layers.",
        "Monitoring targets were left in place so future surveys can compare movement at the repaired joints.",
        "Samples from the bedding mortar were documented before being transferred to the materials laboratory.",
        "The site record recommends seasonal checks after periods of heavy rainfall and rapid temperature change."
    ];

    internal static SeedPdf Generate(int publicationNumber)
    {
        return Generate(GenerateBody(publicationNumber));
    }

    internal static SeedPdf Generate(string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new SeedPdf(body, RenderImageOnlyPdf(body));
    }

    internal static string GenerateBody(int publicationNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(publicationNumber, 1);

        var first = BodySentences[(publicationNumber - 1) % BodySentences.Length];
        var second = BodySentences[(publicationNumber + 3) % BodySentences.Length];
        var third = BodySentences[(publicationNumber + 7) % BodySentences.Length];
        var marker = $"mosaicscan{publicationNumber:D4}";

        return $"This mosaic conservation field record summarizes observations from a documented treatment area. {first} {second} {third} Archive marker {marker}.";
    }

    private static byte[] RenderImageOnlyPdf(string body)
    {
        using var bitmap = new SKBitmap(PageWidth, PageHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = false };
        const float margin = 150;

        using var typeface = LoadTypeface();
        using var headingFont = new SKFont(typeface, 48) { Embolden = true };
        using var bodyFont = new SKFont(typeface, 36);
        canvas.DrawText("MOSAIC CONSERVATION FIELD RECORD", margin, 220,
            SKTextAlign.Left, headingFont, textPaint);

        var y = 320f;
        foreach (var line in WrapText(body, bodyFont, textPaint, PageWidth - (2 * margin)))
        {
            canvas.DrawText(line, margin, y, SKTextAlign.Left, bodyFont, textPaint);
            y += 56;
        }

        canvas.Flush();
        using var image = SKImage.FromBitmap(bitmap);
        using var pdfStream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(pdfStream))
        {
            var pdfCanvas = document.BeginPage(PdfWidth, PdfHeight);
            pdfCanvas.DrawImage(image, new SKRect(0, 0, PdfWidth, PdfHeight),
                new SKSamplingOptions(SKFilterMode.Linear));
            document.EndPage();
            document.Close();
        }

        return pdfStream.ToArray();
    }

    private static SKTypeface LoadTypeface()
    {
        using var fontStream = typeof(SeedPdfGenerator).Assembly
            .GetManifestResourceStream(FontResourceName)
            ?? throw new InvalidOperationException("The embedded seed PDF font is missing.");

        return SKTypeface.FromStream(fontStream)
            ?? throw new InvalidOperationException("The embedded seed PDF font is invalid.");
    }

    private static IEnumerable<string> WrapText(
        string text, SKFont font, SKPaint paint, float maximumWidth)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = words[0];

        foreach (var word in words.Skip(1))
        {
            var candidate = $"{line} {word}";
            if (font.MeasureText(candidate, paint) <= maximumWidth)
            {
                line = candidate;
                continue;
            }

            yield return line;
            line = word;
        }

        yield return line;
    }
}
