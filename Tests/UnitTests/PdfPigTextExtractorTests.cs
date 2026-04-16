using ResearchPublications.Infrastructure.Files;
using Xunit;

namespace ResearchPublications.UnitTests;

public class PdfPigTextExtractorTests
{
    private readonly PdfPigTextExtractor _sut = new();

    [Fact]
    public void Extract_InvalidBytes_ReturnsEmptyString()
    {
        using var stream = new MemoryStream("this is not a pdf"u8.ToArray());

        var result = _sut.Extract(stream);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Extract_EmptyStream_ReturnsEmptyString()
    {
        using var stream = new MemoryStream();

        var result = _sut.Extract(stream);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Extract_ValidMinimalPdf_ReturnsNonNullString()
    {
        using var stream = new MemoryStream(MinimalPdf.Create());

        var result = _sut.Extract(stream);

        Assert.NotNull(result); // may be empty — minimal PDF has no text operators
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static class MinimalPdf
    {
        public static byte[] Create()
        {
            const string header = "%PDF-1.4\n";
            var parts = new[]
            {
                "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
                "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
                "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n",
            };

            var offsets = new int[parts.Length];
            var pos = System.Text.Encoding.ASCII.GetByteCount(header);
            for (var i = 0; i < parts.Length; i++)
            {
                offsets[i] = pos;
                pos += System.Text.Encoding.ASCII.GetByteCount(parts[i]);
            }

            var xrefOffset = pos;
            var xref = new System.Text.StringBuilder();
            xref.Append("xref\n");
            xref.Append($"0 {parts.Length + 1}\n");
            xref.Append("0000000000 65535 f \n");
            foreach (var offset in offsets)
                xref.Append($"{offset:D10} 00000 n \n");
            xref.Append($"trailer\n<< /Size {parts.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

            return System.Text.Encoding.ASCII.GetBytes(header + string.Concat(parts) + xref);
        }
    }
}
