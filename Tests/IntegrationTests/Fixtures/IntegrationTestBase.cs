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
        string? languages = null,
        string? publicationTypes = null,
        string? @abstract = null,
        string? doi = null,
        string? pdfFileName = null,
        List<AuthorDto>? authors = null)
    {
        var payload = new PublicationDetailDto
        {
            Title         = title ?? $"Test-{Guid.NewGuid():N}",
            Year          = year,
            Keywords      = keywords,
            Languages     = languages,
            PublicationTypes = publicationTypes,
            Abstract      = @abstract,
            DOI           = doi,
            PdfFileName   = pdfFileName,
            Authors       = authors ?? [new AuthorDto { FirstName = "Default", LastName = "Author" }]
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
        var response = await Client.GetAsync("/api/authors/filter-options");
        response.EnsureSuccessStatusCode();
        var options = await response.Content.ReadFromJsonAsync<List<FilterOptionResponse>>();
        return (options ?? []).Select(o => o.Name).ToList();
    }

    protected async Task<List<string>> GetAllKeywordValuesAsync()
    {
        var response = await Client.GetAsync("/api/keywords/filter-options");
        response.EnsureSuccessStatusCode();
        var options = await response.Content.ReadFromJsonAsync<List<FilterOptionResponse>>();
        return (options ?? []).Select(o => o.Name).ToList();
    }

    // ── Author management ──────────────────────────────────────────────────

    protected async Task<int> CreateAuthorAsync(
        string? firstName = null,
        string? middleName = null,
        string? lastName = null,
        string? email = null)
    {
        var payload = new AuthorManagementDto
        {
            FirstName  = firstName ?? $"Author-{Guid.NewGuid():N}",
            MiddleName = middleName,
            LastName   = lastName ?? "Test",
            Email      = email
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

    // ── Language management ────────────────────────────────────────────────

    protected async Task<int> CreateLanguageAsync(string? value = null)
    {
        var payload = new LanguageManagementDto
        {
            Value = value ?? $"Language-{Guid.NewGuid():N}"
        };

        var response = await Client.PostAsJsonAsync("/api/languages", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        return result!.Id;
    }

    protected async Task<LanguageManagementDto> GetLanguageAsync(int id)
    {
        var response = await Client.GetAsync($"/api/languages/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LanguageManagementDto>())!;
    }

    protected async Task<LanguageListResponse> ListLanguagesAsync(int page = 1, int pageSize = 100)
    {
        var response = await Client.GetAsync($"/api/languages?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LanguageListResponse>())!;
    }

    // ── Publication type management ────────────────────────────────────────

    protected async Task<int> CreatePublicationTypeAsync(string? value = null)
    {
        var payload = new PublicationTypeManagementDto
        {
            Value = value ?? $"PublicationType-{Guid.NewGuid():N}"
        };

        var response = await Client.PostAsJsonAsync("/api/publication-types", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        return result!.Id;
    }

    protected async Task<PublicationTypeManagementDto> GetPublicationTypeAsync(int id)
    {
        var response = await Client.GetAsync($"/api/publication-types/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublicationTypeManagementDto>())!;
    }

    protected async Task<PublicationTypeListResponse> ListPublicationTypesAsync(int page = 1, int pageSize = 100)
    {
        var response = await Client.GetAsync($"/api/publication-types?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublicationTypeListResponse>())!;
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

    protected record LanguageListResponse(
        List<LanguageManagementDto> Items,
        int Total,
        int Page,
        int PageSize);

    protected record PublicationTypeListResponse(
        List<PublicationTypeManagementDto> Items,
        int Total,
        int Page,
        int PageSize);

    protected record FilterOptionResponse(
        string Name,
        int Count);
}
