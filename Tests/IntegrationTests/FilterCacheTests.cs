using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public sealed class FilterCacheTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicationWrites_NextFilterRequestReturnsCurrentCountsWithoutBrowserCaching(bool delete)
    {
        // Arrange: warm all caches with one linked publication.
        var token = Guid.NewGuid().ToString("N");
        var author = new AuthorDto { FirstName = token, LastName = "Cache" };
        var names = new Dictionary<string, string>
        {
            ["authors"] = $"{token} Cache",
            ["keywords"] = $"Keyword-{token}",
            ["languages"] = $"Language-{token}",
            ["publication-types"] = $"Type-{token}"
        };
        var firstId = await CreateLinkedPublicationAsync();
        await AssertFilterCountsAsync(1);

        // Act / Assert: create invalidates warmed counts, then update/delete does too.
        await CreateLinkedPublicationAsync();
        await AssertFilterCountsAsync(2);
        if (delete)
            (await Client.DeleteAsync($"/api/publications/{firstId}")).EnsureSuccessStatusCode();
        else
            (await Client.PutAsJsonAsync($"/api/publications/{firstId}", new PublicationDetailDto
            {
                Title = $"Unlinked-{token}"
            })).EnsureSuccessStatusCode();
        await AssertFilterCountsAsync(1);

        Task<int> CreateLinkedPublicationAsync() => CreatePublicationAsync(
            keywords: names["keywords"], languages: names["languages"],
            publicationTypes: names["publication-types"], authors: [author]);

        async Task AssertFilterCountsAsync(int expected)
        {
            foreach (var (route, name) in names)
            {
                var response = await Client.GetAsync($"/api/{route}/filter-options");
                response.EnsureSuccessStatusCode();
                Assert.True(response.Headers.CacheControl?.NoStore);
                var options = await response.Content.ReadFromJsonAsync<List<FilterOptionDto>>();
                Assert.Equal(expected, Assert.Single(options!, option => option.Name == name).Count);
            }
        }
    }
}
