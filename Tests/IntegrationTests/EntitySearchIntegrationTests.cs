using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Persistence;
using ResearchPublications.Infrastructure.Search;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("SearchIndexIntegration")]
public sealed class EntitySearchIntegrationTests(SearchIndexApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Startup_IndexesEverySqlEntityIncludingUnusedRecords()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbCntx>();
        var expected = new Dictionary<EntityIndexKind, int>
        {
            [EntityIndexKind.Authors] = await db.Authors.CountAsync(),
            [EntityIndexKind.Keywords] = await db.Keywords.CountAsync(),
            [EntityIndexKind.Languages] = await db.Languages.CountAsync(),
            [EntityIndexKind.PublicationTypes] = await db.PublicationTypes.CountAsync()
        };

        // Act / Assert
        foreach (var (kind, count) in expected)
        {
            var collection = await factory.TypesenseClient.RetrieveCollection(TypesenseEntitySearchIndex.CollectionName(kind));
            Assert.Equal(count, collection.NumberOfDocuments);
        }
    }

    [Theory]
    [InlineData("authors")]
    [InlineData("keywords")]
    [InlineData("languages")]
    [InlineData("publication-types")]
    public async Task AdminSearch_UsesTypesenseForTyposPaginationDropdownsAndCrud(string endpoint)
    {
        // Arrange: no publication references these entities.
        var token = "entity" + Guid.NewGuid().ToString("N");
        var ids = new List<int>();
        try
        {
            for (var i = 0; i < 3; i++)
            {
                var created = await _client.PostAsJsonAsync($"/api/{endpoint}", Payload(endpoint, token, $"Conservation {i}"));
                created.EnsureSuccessStatusCode();
                var result = await created.Content.ReadFromJsonAsync<JsonElement>();
                ids.Add(result.GetProperty("id").GetInt32());
            }

            // Act: a missing letter must still match in both the list and dropdown.
            var first = await SearchAsync(endpoint, $"{token} Conservaton", 1, 2);
            var second = await SearchAsync(endpoint, $"{token} Conservaton", 2, 2);
            var dropdown = await _client.GetFromJsonAsync<JsonElement>(
                $"/api/{endpoint}/search?q={token}%20Conservaton&limit=2");

            // Assert
            Assert.Equal(3, first.GetProperty("total").GetInt32());
            Assert.Equal(2, first.GetProperty("items").GetArrayLength());
            Assert.Single(second.GetProperty("items").EnumerateArray());
            var returnedIds = first.GetProperty("items").EnumerateArray()
                .Concat(second.GetProperty("items").EnumerateArray())
                .Select(item => item.GetProperty("id").GetInt32()).ToHashSet();
            Assert.True(returnedIds.SetEquals(ids));
            Assert.All(first.GetProperty("items").EnumerateArray(), item =>
                Assert.Equal(0, item.GetProperty("publicationCount").GetInt32()));
            Assert.Equal(2, dropdown.GetArrayLength());

            var updated = await _client.PutAsJsonAsync($"/api/{endpoint}/{ids[0]}", Payload(endpoint, token, "Restoration"));
            updated.EnsureSuccessStatusCode();
            var oldMatches = await SearchAsync(endpoint, $"{token} Conservaton");
            Assert.Equal(2, oldMatches.GetProperty("total").GetInt32());
            var newMatches = await SearchAsync(endpoint, $"{token} Restoraton");
            Assert.Equal(ids[0], Assert.Single(newMatches.GetProperty("items").EnumerateArray()).GetProperty("id").GetInt32());

            var deleted = await _client.DeleteAsync($"/api/{endpoint}/{ids[0]}");
            deleted.EnsureSuccessStatusCode();
            ids.RemoveAt(0);
            Assert.Equal(0, (await SearchAsync(endpoint, $"{token} Restoraton")).GetProperty("total").GetInt32());
        }
        finally
        {
            foreach (var id in ids) (await _client.DeleteAsync($"/api/{endpoint}/{id}")).EnsureSuccessStatusCode();
        }
    }

    private Task<JsonElement> SearchAsync(string endpoint, string query, int page = 1, int pageSize = 20) =>
        _client.GetFromJsonAsync<JsonElement>(
            $"/api/{endpoint}?q={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}");

    private static object Payload(string endpoint, string token, string name) => endpoint == "authors"
        ? new { firstName = token, lastName = name, email = $"{token}@example.com" }
        : (object)new { value = $"{token} {name}" };
}
