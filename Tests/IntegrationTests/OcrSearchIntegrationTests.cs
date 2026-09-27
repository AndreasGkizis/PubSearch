using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Search;
using ResearchPublications.Infrastructure.Persistence;
using ResearchPublications.IntegrationTests.Fixtures;
using Typesense;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("SearchIndexIntegration")]
public sealed class OcrSearchIntegrationTests
{
    private readonly SearchIndexApiFactory _factory;
    private readonly HttpClient _client;

    public OcrSearchIntegrationTests(SearchIndexApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OcrSearch_UsesOnlyOcrText_WithPaginationAndHighlights()
    {
        var token = $"ocruniqueterm{Guid.NewGuid():N}";
        var titleOnly = $"titleonly{Guid.NewGuid():N}";
        var ids = new List<int>();
        for (var i = 0; i < 21; i++)
            ids.Add(await CreatePublicationAsync(i == 0 ? titleOnly : $"OCR pagination {token} {i}"));
        await SynchronizeMetadataAsync();

        foreach (var id in ids)
        {
            await _factory.TypesenseClient.UpdateDocument("publications", id.ToString(), new OcrTextUpdate
            {
                OcrText = $"scanned context {token} page {id}"
            });
        }

        var firstPage = await _client.GetFromJsonAsync<OcrSearchResponse>(
            $"/api/ocr-search?q={token}&page=1&pageSize=20");
        var secondPage = await _client.GetFromJsonAsync<OcrSearchResponse>(
            $"/api/ocr-search?q={token}&page=2&pageSize=20");

        Assert.Equal(21, firstPage!.Total);
        Assert.Equal(20, firstPage.Items.Count);
        Assert.All(firstPage.Items, item => Assert.Contains("<mark>", item.OcrSnippet));
        Assert.Single(secondPage!.Items);
        Assert.Equal(2, secondPage.Page);

        var titleOnlyResult = await _client.GetFromJsonAsync<OcrSearchResponse>(
            $"/api/ocr-search?q={titleOnly}");
        Assert.Equal(0, titleOnlyResult!.Total);
    }

    [Fact]
    public async Task DefaultSearch_IncludesOcrMatchesAndPrefersOcrSnippet()
    {
        var token = $"defaultocr{Guid.NewGuid():N}";
        var abstractMatchId = await CreatePublicationAsync(
            $"Abstract match {Guid.NewGuid():N}", abstractText: $"Abstract also contains {token}.");
        var ocrOnlyMatchId = await CreatePublicationAsync($"OCR-only match {Guid.NewGuid():N}");
        await SynchronizeMetadataAsync();
        await _factory.TypesenseClient.UpdateDocument("publications", abstractMatchId.ToString(), new OcrTextUpdate
        {
            OcrText = $"OCR context {token} on scanned page."
        });
        await _factory.TypesenseClient.UpdateDocument("publications", ocrOnlyMatchId.ToString(), new OcrTextUpdate
        {
            OcrText = $"OCR-only context {token} on scanned page."
        });

        var result = await _client.GetFromJsonAsync<RegularSearchResponse>(
            $"/api/search?q={token}");

        Assert.Equal(2, result!.Items.Count);
        Assert.Equal(abstractMatchId, result.Items[0].Id);
        Assert.True(result.Items[0].IsAbstractMatch);
        Assert.Contains(token, result.Items[0].AbstractSnippet);
        Assert.Contains("<mark>", result.Items[0].AbstractSnippet);
        Assert.Contains("OCR context", result.Items[0].OcrSnippet);
        Assert.Contains("<mark>", result.Items[0].OcrSnippet);
        Assert.Equal(ocrOnlyMatchId, result.Items[1].Id);
        Assert.False(result.Items[1].IsAbstractMatch);
        Assert.Contains("OCR-only context", result.Items[1].OcrSnippet);
    }

    [Fact]
    public async Task UnchangedPdf_IsNotOcredTwice()
    {
        await ProcessOcrAsync();
        var pdfFileName = await UploadSeedPdfAsync();
        var id = await CreatePublicationAsync($"OCR unchanged {Guid.NewGuid():N}", pdfFileName);
        await SynchronizeMetadataAsync();
        var before = _factory.OcrProcessor.CallCount;

        await ProcessOcrAsync();
        var afterFirst = _factory.OcrProcessor.CallCount;
        await ProcessOcrAsync();

        Assert.Equal(before + 1, afterFirst);
        Assert.Equal(afterFirst, _factory.OcrProcessor.CallCount);
        var document = await GetDocumentAsync(id);
        Assert.Equal("complete", document.OcrStatus);
    }

    [Fact]
    public async Task ReplacingPdf_RemovesStaleOcrAndIndexesReplacement()
    {
        await ProcessOcrAsync();
        var id = await CreatePublicationAsync(
            $"OCR replacement {Guid.NewGuid():N}", await UploadSeedPdfAsync());
        await SynchronizeMetadataAsync();
        await ProcessOcrAsync();
        var original = await GetDocumentAsync(id);
        await _factory.TypesenseClient.UpdateDocument("publications", id.ToString(), new OcrTextUpdate
        {
            OcrText = "staleocrmarker"
        });

        var replacementFileName = await UploadSeedPdfAsync(modifyContent: true);
        var detail = (await _client.GetFromJsonAsync<PublicationDetailDto>($"/api/publications/{id}"))!;
        var update = await _client.PutAsJsonAsync($"/api/publications/{id}", detail with
        {
            PdfFileName = replacementFileName
        });
        update.EnsureSuccessStatusCode();
        await SynchronizeMetadataAsync();
        _factory.OcrProcessor.ReturnNext("replacementocrmarker");

        await ProcessOcrAsync();

        var replaced = await GetDocumentAsync(id);
        Assert.DoesNotContain("staleocrmarker", replaced.OcrText);
        Assert.Contains("replacementocrmarker", replaced.OcrText);
        Assert.Equal(replacementFileName, replaced.OcrSourceFileName);
        Assert.NotEqual(original.OcrSourceHash, replaced.OcrSourceHash);
    }

    [Fact]
    public async Task OcrFailure_LeavesSqlAndMetadataSearchAvailable()
    {
        await ProcessOcrAsync();
        var title = $"OCR failure metadata {Guid.NewGuid():N}";
        var pdfFileName = await UploadSeedPdfAsync();
        var id = await CreatePublicationAsync(title, pdfFileName);
        await SynchronizeMetadataAsync();
        _factory.OcrProcessor.FailNext("simulated Ollama failure");

        await ProcessOcrAsync();

        var failed = await GetDocumentAsync(id);
        Assert.Equal("failed", failed.OcrStatus);
        Assert.Equal(string.Empty, failed.OcrText);
        Assert.Equal(title, failed.Title);

        var detail = await _client.GetFromJsonAsync<PublicationDetailDto>($"/api/publications/{id}");
        Assert.True(string.IsNullOrEmpty(detail!.Body));
        var metadataSearch = await _client.GetFromJsonAsync<RegularSearchResponse>(
            $"/api/search?q={Uri.EscapeDataString(title)}");
        Assert.Contains(metadataSearch!.Items, item => item.Id == id);
    }

    private async Task ProcessOcrAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IOcrIndexingService>().ProcessPendingAsync();
    }

    private async Task SynchronizeMetadataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var result = await scope.ServiceProvider
            .GetRequiredService<ITypesensePublicationIndexService>()
            .SynchronizeFromSqlAsync();
        Assert.True(result.Success, result.ErrorMessage);
    }

    private async Task<string> UploadSeedPdfAsync(bool modifyContent = false)
    {
        var bytes = SeedPdfGenerator.Generate(1).PdfBytes;
        if (modifyContent) bytes = [.. bytes, (byte)'\n'];
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "replacement.pdf");
        var response = await _client.PostAsync("/api/publications/upload", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UploadResponse>())!.FileName;
    }

    private async Task<int> CreatePublicationAsync(string title, string? pdfFileName = null, string? abstractText = null)
    {
        var response = await _client.PostAsJsonAsync("/api/publications", new PublicationDetailDto
        {
            Title = title,
            PdfFileName = pdfFileName,
            Abstract = abstractText,
            Authors = [new AuthorDto { FirstName = "OCR", LastName = "Test" }]
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateResponse>())!.Id;
    }

    private Task<PublicationDocument> GetDocumentAsync(int id) =>
        _factory.TypesenseClient.RetrieveDocument<PublicationDocument>("publications", id.ToString());

    private sealed class OcrTextUpdate
    {
        [JsonPropertyName("ocr_text")]
        public string OcrText { get; init; } = string.Empty;
    }

    private sealed record UploadResponse(string FileName);
    private sealed record CreateResponse(int Id);
    private sealed record OcrSearchResponse(
        List<OcrSearchResultDto> Items,
        int Total,
        int Page,
        int PageSize,
        long ElapsedMs);
    private sealed record RegularSearchResponse(List<SearchResultDto> Items);
}
