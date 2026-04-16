using System.Net;
using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public class PdfUploadTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    // ── Upload endpoint ────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_ValidPdf_ReturnsFileNameAndExtractedText()
    {
        var result = await UploadPdfAsync();

        Assert.False(string.IsNullOrWhiteSpace(result.FileName));
        Assert.NotNull(result.ExtractedText); // may be empty string for a no-content PDF, but must not be absent
    }

    [Fact]
    public async Task Upload_ValidPdf_FileExistsOnDisk()
    {
        var result = await UploadPdfAsync();

        var fullPath = Path.Combine(Factory.PdfStoragePath, result.FileName);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task Upload_NonPdfFile_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("not a pdf"u8.ToArray()), "file", "document.txt");
        var response = await Client.PostAsync("/api/publications/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Discard (DELETE files) endpoint ───────────────────────────────────

    [Fact]
    public async Task DiscardUpload_ValidFileName_RemovesFileFromDisk()
    {
        var upload = await UploadPdfAsync();
        var fullPath = Path.Combine(Factory.PdfStoragePath, upload.FileName);
        Assert.True(File.Exists(fullPath)); // sanity check

        var response = await Client.DeleteAsync($"/api/publications/files/{Uri.EscapeDataString(upload.FileName)}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task DiscardUpload_EmptyFileName_ReturnsBadRequest()
    {
        // Route won't match an empty segment; use a whitespace-encoded value to exercise validation
        var response = await Client.DeleteAsync("/api/publications/files/%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscardUpload_NonExistentFileName_ReturnsNoContent()
    {
        // Discarding a file that doesn't exist should be idempotent, not an error
        var response = await Client.DeleteAsync($"/api/publications/files/{Guid.NewGuid():N}_ghost.pdf");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Publication delete ─────────────────────────────────────────────────

    [Fact]
    public async Task DeletePublication_WithPdf_DeletesPdfFromDisk()
    {
        var upload = await UploadPdfAsync();
        var pubId = await CreatePublicationWithPdfAsync(upload.FileName);
        var fullPath = Path.Combine(Factory.PdfStoragePath, upload.FileName);
        Assert.True(File.Exists(fullPath)); // sanity check

        var response = await Client.DeleteAsync($"/api/publications/{pubId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeletePublication_WithoutPdf_Succeeds()
    {
        var id = await CreatePublicationAsync(title: $"NoPdf-{Guid.NewGuid():N}");

        var response = await Client.DeleteAsync($"/api/publications/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Publication update ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePublication_WithReplacedPdf_DeletesOldPdfFromDisk()
    {
        var oldUpload = await UploadPdfAsync("old.pdf");
        var pubId = await CreatePublicationWithPdfAsync(oldUpload.FileName);
        var oldPath = Path.Combine(Factory.PdfStoragePath, oldUpload.FileName);
        Assert.True(File.Exists(oldPath)); // sanity check

        var newUpload = await UploadPdfAsync("new.pdf");
        var updatePayload = new PublicationDetailDto
        {
            Title       = "Updated with new PDF",
            PdfFileName = newUpload.FileName,
            Authors     = [new AuthorDto { FirstName = "PDF", LastName = "Author" }]
        };
        var response = await Client.PutAsJsonAsync($"/api/publications/{pubId}", updatePayload);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Assert.False(File.Exists(oldPath));
        Assert.True(File.Exists(Path.Combine(Factory.PdfStoragePath, newUpload.FileName)));
    }

    [Fact]
    public async Task UpdatePublication_WithSamePdf_DoesNotDeleteFile()
    {
        var upload = await UploadPdfAsync();
        var pubId = await CreatePublicationWithPdfAsync(upload.FileName);
        var fullPath = Path.Combine(Factory.PdfStoragePath, upload.FileName);

        var updatePayload = new PublicationDetailDto
        {
            Title       = "Updated title, same PDF",
            PdfFileName = upload.FileName,
            Authors     = [new AuthorDto { FirstName = "PDF", LastName = "Author" }]
        };
        var response = await Client.PutAsJsonAsync($"/api/publications/{pubId}", updatePayload);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Assert.True(File.Exists(fullPath));
    }
}
