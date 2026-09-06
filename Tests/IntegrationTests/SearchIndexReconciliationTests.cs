using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Persistence;
using ResearchPublications.Infrastructure.Search;
using ResearchPublications.IntegrationTests.Fixtures;
using Typesense;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("SearchIndexIntegration")]
public sealed class SearchIndexReconciliationTests
{
    private readonly SearchIndexApiFactory _factory;
    private readonly HttpClient _client;

    public SearchIndexReconciliationTests(SearchIndexApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Startup_ImportsAllSqlPublications()
    {
        using var scope = _factory.Services.CreateScope();
        var sqlCount = scope.ServiceProvider.GetRequiredService<AppDbCntx>().Publications.Count();

        var documents = await _factory.TypesenseClient.ExportDocuments<PublicationDocument>("publications");

        Assert.Equal(sqlCount, documents.Count);
        Assert.All(documents, document => Assert.NotEmpty(document.ContentHash));
    }

    [Fact]
    public async Task Worker_SynchronizesPublicationCreateEditAndDelete()
    {
        var id = await CreatePublicationAsync(title: $"Created-{Guid.NewGuid():N}");
        var created = await WaitForDocumentAsync(id, document => document is not null);
        Assert.StartsWith("Created-", created!.Title);

        var detail = await GetPublicationAsync(id);
        detail = detail with { Title = $"Edited-{Guid.NewGuid():N}" };
        var update = await _client.PutAsJsonAsync($"/api/publications/{id}", detail);
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var edited = await WaitForDocumentAsync(id, document => document?.Title == detail.Title);
        Assert.Equal(detail.Title, edited!.Title);

        var delete = await _client.DeleteAsync($"/api/publications/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Null(await WaitForDocumentAsync(id, document => document is null));
    }

    [Fact]
    public async Task Worker_SynchronizesRelatedEntityRenamesAndDeletion()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var id = await CreatePublicationAsync(
            title: $"Related-{suffix}",
            keywords: $"Keyword-{suffix}",
            languages: $"Language-{suffix}",
            publicationTypes: $"Type-{suffix}",
            authors: [new AuthorDto { FirstName = $"Author-{suffix}", LastName = "Original" }]);
        var detail = await GetPublicationAsync(id);
        await WaitForDocumentAsync(id, document => document is not null);

        var author = detail.Authors.Single() with { LastName = "Renamed" };
        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.PutAsJsonAsync($"/api/authors/{author.Id}", author)).StatusCode);

        var keyword = await FindValueEntityAsync("keywords", $"Keyword-{suffix}");
        var language = await FindValueEntityAsync("languages", $"Language-{suffix}");
        var publicationType = await FindValueEntityAsync("publication-types", $"Type-{suffix}");
        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.PutAsJsonAsync($"/api/keywords/{keyword.Id}", new { value = $"Keyword-Renamed-{suffix}" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.PutAsJsonAsync($"/api/languages/{language.Id}", new { value = $"Language-Renamed-{suffix}" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.PutAsJsonAsync($"/api/publication-types/{publicationType.Id}", new { value = $"Type-Renamed-{suffix}" })).StatusCode);

        var renamed = await WaitForDocumentAsync(id, document =>
            document is not null
            && document.Authors.Contains($"Author-{suffix} Renamed")
            && document.Keywords.Contains($"Keyword-Renamed-{suffix}")
            && document.Languages.Contains($"Language-Renamed-{suffix}")
            && document.PublicationTypes.Contains($"Type-Renamed-{suffix}"));
        Assert.NotNull(renamed);

        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.DeleteAsync($"/api/keywords/{keyword.Id}")).StatusCode);
        var afterDelete = await WaitForDocumentAsync(id,
            document => document is not null && !document.Keywords.Contains($"Keyword-Renamed-{suffix}"));
        Assert.DoesNotContain($"Keyword-Renamed-{suffix}", afterDelete!.Keywords);
    }

    [Fact]
    public async Task SynchronizeFromSql_DoesNotRewriteUnchangedDocuments()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITypesensePublicationIndexService>();

        await service.SynchronizeFromSqlAsync();
        var result = await service.SynchronizeFromSqlAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.Added);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Deleted);
        Assert.True(result.Unchanged > 0);
    }

    [Fact]
    public async Task TypesenseOutage_DoesNotBreakSqlCrud_AndRecoveryCatchesUp()
    {
        await _factory.StopTypesenseAsync();
        int id;
        try
        {
            id = await CreatePublicationAsync(title: $"Outage-{Guid.NewGuid():N}");
            Assert.NotNull(await GetPublicationAsync(id));
        }
        finally
        {
            await _factory.StartTypesenseAsync();
        }

        var recovered = await WaitForDocumentAsync(id, document => document is not null, TimeSpan.FromSeconds(15));
        Assert.NotNull(recovered);
    }

    [Fact]
    public async Task ManualRebuild_RemovesOrphanedDocuments()
    {
        const string orphanId = "2147483647";
        await _factory.TypesenseClient.UpsertDocument("publications", new PublicationDocument
        {
            Id = orphanId,
            Title = "Orphan",
            Authors = [],
            Keywords = [],
            Languages = [],
            PublicationTypes = [],
            ContentHash = "orphan"
        });

        var response = await _client.PostAsync("/api/search/rebuild-index", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SearchIndexReconciliationResult>();

        Assert.True(result!.Success);
        Assert.True(result.Deleted > 0);
        await Assert.ThrowsAsync<TypesenseApiNotFoundException>(() =>
            _factory.TypesenseClient.RetrieveDocument<PublicationDocument>("publications", orphanId));
    }

    private async Task<int> CreatePublicationAsync(
        string title,
        string? keywords = null,
        string? languages = null,
        string? publicationTypes = null,
        List<AuthorDto>? authors = null)
    {
        var response = await _client.PostAsJsonAsync("/api/publications", new PublicationDetailDto
        {
            Title = title,
            Keywords = keywords,
            Languages = languages,
            PublicationTypes = publicationTypes,
            Authors = authors ?? [new AuthorDto { FirstName = "Index", LastName = "Test" }]
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateResponse>())!.Id;
    }

    private async Task<PublicationDetailDto> GetPublicationAsync(int id) =>
        (await _client.GetFromJsonAsync<PublicationDetailDto>($"/api/publications/{id}"))!;

    private async Task<ValueEntity> FindValueEntityAsync(string route, string value)
    {
        var matches = await _client.GetFromJsonAsync<List<ValueEntity>>(
            $"/api/{route}/search?q={Uri.EscapeDataString(value)}&limit=5");
        return matches!.Single(item => item.Value == value);
    }

    private async Task<PublicationDocument?> WaitForDocumentAsync(
        int id,
        Func<PublicationDocument?, bool> predicate,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(7));
        while (DateTime.UtcNow < deadline)
        {
            PublicationDocument? document = null;
            try
            {
                document = await _factory.TypesenseClient.RetrieveDocument<PublicationDocument>(
                    "publications", id.ToString());
            }
            catch (TypesenseApiNotFoundException)
            {
                // Expected while the worker catches up or after deletion.
            }
            catch (TypesenseApiUnprocessableEntityException)
            {
                // Typesense can briefly report "Not Ready or Lagging" after restart.
            }
            catch (TypesenseApiServiceUnavailableException)
            {
                // Expected while Typesense completes restart.
            }

            if (predicate(document)) return document;
            await Task.Delay(250);
        }

        throw new TimeoutException($"Typesense document {id} did not reach the expected state.");
    }

    private sealed record CreateResponse(int Id);
    private sealed record ValueEntity(int Id, string Value);
}
