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

    // ── Author management ──────────────────────────────────────────────────

    protected async Task<int> CreateAuthorAsync(
        string? fullName = null,
        string? firstName = null,
        string? lastName = null,
        string? email = null)
    {
        var payload = new AuthorManagementDto
        {
            FullName  = fullName ?? $"Author-{Guid.NewGuid():N}",
            FirstName = firstName ?? "",
            LastName  = lastName ?? "",
            Email     = email
        };

        var response = await Client.PostAsJsonAsync("/api/authors", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        return result!.Id;
    }

    protected async Task<AuthorManagementDto> GetAuthorAsync(int id)
    {
        var response = await Client.GetAsync($"/api/authors/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthorManagementDto>())!;
    }

    protected async Task<AuthorListResponse> ListAuthorsAsync(int page = 1, int pageSize = 100)
    {
        var response = await Client.GetAsync($"/api/authors?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthorListResponse>())!;
    }

    // ── Keyword management ─────────────────────────────────────────────────

    protected async Task<int> CreateKeywordAsync(string? value = null)
    {
        var payload = new KeywordManagementDto
        {
            Value = value ?? $"Keyword-{Guid.NewGuid():N}"
        };

        var response = await Client.PostAsJsonAsync("/api/keywords", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        return result!.Id;
    }

    protected async Task<KeywordManagementDto> GetKeywordAsync(int id)
    {
        var response = await Client.GetAsync($"/api/keywords/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KeywordManagementDto>())!;
    }

    protected async Task<KeywordListResponse> ListKeywordsAsync(int page = 1, int pageSize = 100)
    {
        var response = await Client.GetAsync($"/api/keywords?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KeywordListResponse>())!;
    }

    // ── Response DTOs ──────────────────────────────────────────────────────

    protected record CreateResponse(int Id);

    protected record ListResponse(
        List<PublicationSummaryDto> Items,
        int Total,
        int Page,
        int PageSize);

    protected record AuthorListResponse(
        List<AuthorManagementDto> Items,
        int Total,
        int Page,
        int PageSize);

    protected record KeywordListResponse(
        List<KeywordManagementDto> Items,
        int Total,
        int Page,
        int PageSize);
}
