using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public class PublicationWorkflowTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UploadPdf_ThenCreatePublication_DownloadReturnsSameFile()
    {
        // Arrange
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF");
        using var file = new ByteArrayContent(pdfBytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");

        using var form = new MultipartFormDataContent();
        form.Add(file, "file", "sample.pdf");

        // Act — upload
        var uploadResponse = await Client.PostAsync("/api/publications/upload", form);

        // Assert — upload success
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var upload = await uploadResponse.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.False(string.IsNullOrWhiteSpace(upload?.FileName));

        // Act — create publication that references uploaded PDF
        var id = await CreatePublicationAsync(
            title: $"PdfFlow-{Guid.NewGuid():N}",
            pdfFileName: upload!.FileName,
            authors: [new AuthorDto { FirstName = "Pdf", LastName = "Author" }]);

        // Act — download PDF
        var downloadResponse = await Client.GetAsync($"/api/publications/{id}/download");

        // Assert — same file bytes are returned
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(pdfBytes, downloadedBytes);
    }

    [Fact]
    public async Task Publication_HappyPath_CreateSearchEditDelete_WorksEndToEnd()
    {
        // Arrange related entities
        var marker = Guid.NewGuid().ToString("N");
        var authorFirstName = $"FlowAuthor-{marker}";
        var authorLastName = "Tester";
        var authorFullName = $"{authorFirstName} {authorLastName}";

        var keywordA = $"FlowKeywordA-{marker}";
        var keywordB = $"FlowKeywordB-{marker}";
        var language = $"FlowLanguage-{marker}";
        var publicationType = $"FlowType-{marker}";

        var authorId = await CreateAuthorAsync(firstName: authorFirstName, lastName: authorLastName);
        await CreateKeywordAsync(keywordA);
        await CreateKeywordAsync(keywordB);
        await CreateLanguageAsync(language);
        await CreatePublicationTypeAsync(publicationType);

        var originalTitle = $"FlowTitle-{marker}";
        var updatedTitle = $"FlowTitleUpdated-{marker}";

        // Create publication
        var createPayload = new PublicationDetailDto
        {
            Title = originalTitle,
            Year = 2024,
            DOI = $"10.1000/{marker[..12]}",
            Abstract = "Initial abstract",
            Body = "Initial body",
            Keywords = keywordA,
            Languages = language,
            PublicationTypes = publicationType,
            Authors = [new AuthorDto
            {
                Id = authorId,
                FirstName = authorFirstName,
                LastName = authorLastName
            }]
        };

        var createResponse = await Client.PostAsJsonAsync("/api/publications", createPayload);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateResponse>();
        Assert.NotNull(created);
        var publicationId = created!.Id;

        // Find publication via search (SQL provider)
        var searchUrl =
            "/api/search?q=" + Uri.EscapeDataString(originalTitle) +
            "&provider=mssql" +
            "&authors=" + Uri.EscapeDataString(authorFullName) +
            "&keywords=" + Uri.EscapeDataString(keywordA) +
            "&languages=" + Uri.EscapeDataString(language) +
            "&publicationTypes=" + Uri.EscapeDataString(publicationType);

        var searchResponse = await Client.GetAsync(searchUrl);
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchResult = await searchResponse.Content.ReadFromJsonAsync<SearchResponse>();
        Assert.NotNull(searchResult);
        Assert.Contains(searchResult!.Items, i => i.Id == publicationId);

        // Edit publication
        var updatePayload = createPayload with
        {
            Title = updatedTitle,
            Keywords = keywordB,
            Abstract = "Updated abstract",
            Body = "Updated body"
        };

        var updateResponse = await Client.PutAsJsonAsync($"/api/publications/{publicationId}", updatePayload);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var detail = await GetPublicationAsync(publicationId);
        Assert.Equal(updatedTitle, detail.Title);
        Assert.Contains(keywordB, detail.Keywords ?? string.Empty);
        Assert.DoesNotContain(keywordA, detail.Keywords ?? string.Empty);
        Assert.Equal("Updated abstract", detail.Abstract);
        Assert.Equal("Updated body", detail.Body);

        // Delete publication
        var deleteResponse = await Client.DeleteAsync($"/api/publications/{publicationId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await Client.GetAsync($"/api/publications/{publicationId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    private sealed record UploadResponse(string FileName);

    private sealed record SearchResponse(
        List<SearchResultDto> Items,
        int Total,
        int Page,
        int PageSize,
        string Provider,
        long ElapsedMs);
}

