using ResearchPublications.Infrastructure.Ocr;

namespace ResearchPublications.UnitTests.Infrastructure;

public sealed class OcrMarkdownConverterTests
{
    [Fact]
    public void ToMarkdown_UnwrapsMarkdownFenceBeforeConvertingRegions()
    {
        // Arrange
        const string input = "```markdown\ntitle [1, 2, 3, 4]Title\ntext [1, 2, 3, 4]Body\n```";

        // Act
        var markdown = OcrMarkdownConverter.ToMarkdown(input);

        // Assert
        Assert.Equal("# Title\n\nBody", markdown);
    }

    [Fact]
    public void ToMarkdown_ConvertsRegionsAndPreservesContentCoordinates()
    {
        // Arrange
        const string input = "title [86, 80, 658, 105]MOSAIC CONSERVATION FIELD RECORD\n" +
            "text [84, 130, 900, 175]Scanned page marker dualfieldquartz.\n" +
            "text [1, 2, 3, 4]Measurements [12, 123, 123, 456] and [illegible].";

        // Act
        var markdown = OcrMarkdownConverter.ToMarkdown(input);

        // Assert
        Assert.Equal("# MOSAIC CONSERVATION FIELD RECORD\n\n" +
            "Scanned page marker dualfieldquartz.\n\n" +
            "Measurements [12, 123, 123, 456] and [illegible].", markdown);
    }

    [Fact]
    public void ToMarkdown_PreservesExistingMarkdown()
    {
        // Arrange
        const string input = "# Title\n\nA [link](https://example.com).\n\n| A | B |\n| --- | --- |\n| 1 | 2 |";

        // Act
        var markdown = OcrMarkdownConverter.ToMarkdown(input);

        // Assert
        Assert.Equal(input, markdown);
    }
}
