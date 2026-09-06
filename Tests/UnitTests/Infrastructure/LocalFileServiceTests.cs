using System.Text;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using ResearchPublications.Infrastructure.Files;

namespace ResearchPublications.UnitTests.Infrastructure;

public sealed class LocalFileServiceTests : IDisposable
{
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"pubsearch-unit-{Guid.NewGuid():N}");

    [Fact]
    public async Task SavePdf_PathInOriginalName_StoresOnlyTheFileNameInsideConfiguredFolder()
    {
        // Arrange
        var service = CreateService();
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("test PDF content"));

        // Act
        var storedName = await service.SavePdfAsync(content, "../../paper.pdf");

        // Assert
        Assert.EndsWith("_paper.pdf", storedName);
        Assert.Equal(storedName, Path.GetFileName(storedName));
        Assert.True(service.Exists(storedName));
        await using var storedFile = await service.GetPdfAsync(storedName);
        using var reader = new StreamReader(storedFile!);
        Assert.Equal("test PDF content", await reader.ReadToEndAsync());
    }

    [Fact]
    public void Exists_EmptyFileName_RejectsTheRequest()
    {
        // Arrange
        var service = CreateService();

        // Act
        void Action() => service.Exists(" ");

        // Assert
        Assert.Throws<ArgumentException>(Action);
    }

    private LocalFileService CreateService()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["PdfStorage:Path"].Returns(_storagePath);
        return new LocalFileService(configuration);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
            Directory.Delete(_storagePath, recursive: true);
    }
}
