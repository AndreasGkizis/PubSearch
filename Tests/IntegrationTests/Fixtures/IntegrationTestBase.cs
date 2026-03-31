using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;

namespace ResearchPublications.IntegrationTests.Fixtures;

/// <summary>
/// Shared helpers every integration test class inherits.
/// Provides a pre-configured HttpClient and convenience methods
/// for the /api/publications endpoints.
/// </summary>
public abstract class IntegrationTestBase
{
    protected HttpClient Client { get; }

    protected IntegrationTestBase(PubSearchApiFactory factory)
    {
        Client = factory.CreateClient();
    }

    // ── Create ─────────────────────────────────────────────────────────────

    protected async Task<int> CreatePublicationAsync(
        string? title = null,
        int? year = 2024,
        string? keywords = null,
        string? @abstract = null,
        string? doi = null,
        List<AuthorDto>? authors = null)
    {
        var payload = new PublicationDetailDto
        {
            Title         = title ?? $"Test-{Guid.NewGuid():N}",
            Year          = year,
            Keywords      = keywords,
            Abstract      = @abstract,
            DOI           = doi,
            Authors       = authors ?? [new AuthorDto { FullName = "Default Author" }]
        };

        var response = await Client.PostAsJsonAsync("/api/publications", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        return result!.Id;
    }

    // ── Read ───────────────────────────────────────────────────────────────

    protected async Task<PublicationDetailDto> GetPublicationAsync(int id)
    {
        var response = await Client.GetAsync($"/api/publications/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublicationDetailDto>())!;
    }

    protected async Task<ListResponse> ListPublicationsAsync(int page = 1, int pageSize = 100)
    {
        var response = await Client.GetAsync($"/api/publications?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ListResponse>())!;
    }

    // ── Lookup endpoints ───────────────────────────────────────────────────

    protected async Task<List<string>> GetAllAuthorNamesAsync()
    {
        var response = await Client.GetAsync("/api/publications/authors");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<string>>())!;
    }

    protected async Task<List<string>> GetAllKeywordValuesAsync()
    {
        var response = await Client.GetAsync("/api/publications/keywords");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<string>>())!;
    }

    // ── Response DTOs ──────────────────────────────────────────────────────

    protected record CreateResponse(int Id);

    protected record ListResponse(
        List<PublicationSummaryDto> Items,
        int Total,
        int Page,
        int PageSize);
}
