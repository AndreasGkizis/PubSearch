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
    protected PubSearchApiFactory Factory { get; }

    protected IntegrationTestBase(PubSearchApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    // ── PDF upload helpers ─────────────────────────────────────────────────

    /// <summary>Uploads a minimal valid PDF and returns the parsed response.</summary>
    protected async Task<PdfUploadResponse> UploadPdfAsync(string fileName = "test.pdf")
    {
        var pdfBytes = MinimalPdf.Create();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(pdfBytes) { Headers = { ContentType = new("application/pdf") } }, "file", fileName);
        var response = await Client.PostAsync("/api/publications/upload", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PdfUploadResponse>())!;
    }

    protected async Task<int> CreatePublicationWithPdfAsync(string pdfFileName)
    {
        var payload = new PublicationDetailDto
        {
            Title       = $"PDF-Pub-{Guid.NewGuid():N}",
            PdfFileName = pdfFileName,
            Authors     = [new AuthorDto { FirstName = "PDF", LastName = "Author" }]
        };
        var response = await Client.PostAsJsonAsync("/api/publications", payload);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        return result!.Id;
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
        return (await response.Content.ReadFromJsonAsync<List<string>>())!;
    }

    protected async Task<List<string>> GetAllKeywordValuesAsync()
    {
        var response = await Client.GetAsync("/api/keywords/filter-options");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<string>>())!;
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

    protected record PdfUploadResponse(string FileName, string? ExtractedText);

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Generates a valid single-page PDF with no text content.</summary>
    protected static class MinimalPdf
    {
        public static byte[] Create()
        {
            const string header = "%PDF-1.4\n";
            var parts = new[]
            {
                "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
                "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
                "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n",
            };

            var offsets = new int[parts.Length];
            var pos = System.Text.Encoding.ASCII.GetByteCount(header);
            for (var i = 0; i < parts.Length; i++)
            {
                offsets[i] = pos;
                pos += System.Text.Encoding.ASCII.GetByteCount(parts[i]);
            }

            var xrefOffset = pos;
            var xref = new System.Text.StringBuilder();
            xref.Append("xref\n");
            xref.Append($"0 {parts.Length + 1}\n");
            xref.Append("0000000000 65535 f \n");
            foreach (var offset in offsets)
                xref.Append($"{offset:D10} 00000 n \n");
            xref.Append($"trailer\n<< /Size {parts.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

            var body = header + string.Concat(parts) + xref;
            return System.Text.Encoding.ASCII.GetBytes(body);
        }
    }
}
