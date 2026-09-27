using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Infrastructure.Persistence;
using ResearchPublications.Infrastructure.Search;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("SearchIndexIntegration")]
public sealed class UiSearchFixtureTests(SearchIndexApiFactory factory)
{
    private const string CollectionName = "publications";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Fixtures_AreAddedToExistingDatabaseAndSeedingIsIdempotent()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbCntx>();
        var initialCount = await db.Publications.CountAsync();

        Assert.Equal(10, await db.Publications.CountAsync(
            publication => publication.Title.StartsWith("UI Fixture ")));

        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();

        Assert.Equal(initialCount, await db.Publications.CountAsync());
        Assert.Equal(10, await db.Publications.CountAsync(
            publication => publication.Title.StartsWith("UI Fixture ")));
    }

    [Fact]
    public async Task Fixtures_CoverMetadataBodyOcrAndFiltersInDefaultSearch()
    {
        var abstractMatch = await SearchSingleAsync("abstractonlyamber");
        Assert.True(abstractMatch.IsAbstractMatch);
        Assert.Contains("<mark>", abstractMatch.AbstractSnippet);
        Assert.Null(abstractMatch.OcrSnippet);

        var bodyMatch = await SearchSingleAsync("bodyonlycobalt");
        Assert.False(bodyMatch.IsAbstractMatch);
        Assert.Contains("<mark>", bodyMatch.AbstractSnippet);

        var titleMatch = await SearchSingleAsync("titleonlycoral");
        Assert.Contains("titleonlycoral", titleMatch.Title, StringComparison.OrdinalIgnoreCase);

        var keywordMatch = await SearchSingleAsync("keywordonlycedar");
        Assert.Contains("<mark>", keywordMatch.HighlightedKeywords);

        var authorMatch = await SearchSingleAsync("authoronlylapis");
        Assert.Contains(authorMatch.HighlightedAuthors!, name => name.Contains("<mark>"));

        var filtered = await _client.GetFromJsonAsync<SearchResponse>(
            "/api/search?q=filteronlycopper&yearFrom=2020&yearTo=2020&languages=French&publicationTypes=Report");
        Assert.Contains(filtered!.Items, item => item.Title == "UI Fixture 10 - Filter Match");

        await SetOcrTextAsync("UI Fixture 03 - OCR Only", "The scanned page contains ocronlyjade.");
        var ocrMatch = await SearchSingleAsync("ocronlyjade");
        Assert.False(ocrMatch.IsAbstractMatch);
        Assert.Contains("<mark>", ocrMatch.OcrSnippet);

        await SetOcrTextAsync("UI Fixture 04 - Abstract and OCR", "Scanned page marker dualfieldquartz.");
        var dualMatch = await SearchSingleAsync("dualfieldquartz");
        Assert.True(dualMatch.IsAbstractMatch);
        Assert.Contains("<mark>", dualMatch.AbstractSnippet);
        Assert.Contains("<mark>", dualMatch.OcrSnippet);

        await SetOcrTextAsync("UI Fixture 06 - OCR Ranking Match", "Scanned page marker rankingsapphire.");
        var rankingResults = await _client.GetFromJsonAsync<SearchResponse>("/api/search?q=rankingsapphire");
        Assert.Equal(
            ["UI Fixture 05 - Abstract Ranking Match", "UI Fixture 06 - OCR Ranking Match"],
            rankingResults!.Items.Select(item => item.Title));
    }

    private async Task<SearchResultDto> SearchSingleAsync(string query)
    {
        var result = await _client.GetFromJsonAsync<SearchResponse>($"/api/search?q={query}");
        return Assert.Single(result!.Items);
    }

    private async Task SetOcrTextAsync(string title, string ocrText)
    {
        var document = (await factory.TypesenseClient.ExportDocuments<PublicationDocument>(CollectionName))
            .Single(item => item.Title == title);
        await factory.TypesenseClient.UpdateDocument(CollectionName, document.Id, new OcrTextUpdate
        {
            OcrText = ocrText
        });
    }

    private sealed class OcrTextUpdate
    {
        [JsonPropertyName("ocr_text")]
        public string OcrText { get; init; } = string.Empty;
    }

    private sealed record SearchResponse(List<SearchResultDto> Items);
}
