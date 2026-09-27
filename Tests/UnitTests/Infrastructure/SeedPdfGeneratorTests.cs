using System.Text;
using ResearchPublications.Infrastructure.Persistence;

namespace ResearchPublications.UnitTests.Infrastructure;

public sealed class SeedPdfGeneratorTests
{
    [Fact]
    public void GenerateBody_SamePublicationNumber_ReturnsSameRelevantBody()
    {
        // Arrange
        const int publicationNumber = 37;

        // Act
        var first = SeedPdfGenerator.GenerateBody(publicationNumber);
        var second = SeedPdfGenerator.GenerateBody(publicationNumber);

        // Assert
        Assert.Equal(first, second);
        Assert.Contains("mosaic conservation", first, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mosaicscan0037", first, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateBody_AllSeededPublications_HaveUniqueOcrMarkers()
    {
        // Arrange
        var publicationNumbers = Enumerable.Range(1, 150);

        // Act
        var bodies = publicationNumbers.Select(SeedPdfGenerator.GenerateBody).ToList();

        // Assert
        Assert.Equal(150, bodies.Distinct().Count());
        for (var index = 0; index < bodies.Count; index++)
            Assert.Contains($"mosaicscan{index + 1:D4}", bodies[index], StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_CreatesPdfWithoutEmbeddedSourceText()
    {
        // Arrange
        const int publicationNumber = 1;

        // Act
        var document = SeedPdfGenerator.Generate(publicationNumber);
        var pdfContent = Encoding.Latin1.GetString(document.PdfBytes);

        // Assert
        Assert.StartsWith("%PDF-", pdfContent, StringComparison.Ordinal);
        Assert.Contains("%%EOF", pdfContent, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Image", pdfContent, StringComparison.Ordinal);
        Assert.DoesNotContain(document.Body, pdfContent, StringComparison.Ordinal);
        Assert.DoesNotContain("mosaicscan0001", pdfContent, StringComparison.Ordinal);
    }
}
