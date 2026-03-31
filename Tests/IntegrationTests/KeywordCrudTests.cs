using System.Net;
using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public class KeywordCrudTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_ValidKeyword_ReturnsCreatedWithId()
    {
        var payload = new KeywordManagementDto { Value = $"KwCreate-{Guid.NewGuid():N}" };

        var response = await Client.PostAsJsonAsync("/api/keywords", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        Assert.True(result!.Id > 0);
    }

    [Fact]
    public async Task Create_DuplicateValue_ReturnsError()
    {
        var value = $"DupKw-{Guid.NewGuid():N}";
        await CreateKeywordAsync(value: value);

        var payload = new KeywordManagementDto { Value = value };
        var response = await Client.PostAsJsonAsync("/api/keywords", payload);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingKeyword_ReturnsDetail()
    {
        var value = $"KwGet-{Guid.NewGuid():N}";
        var id = await CreateKeywordAsync(value: value);

        var detail = await GetKeywordAsync(id);

        Assert.Equal(value, detail.Value);
        Assert.Equal(0, detail.PublicationCount);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await Client.GetAsync("/api/keywords/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResults()
    {
        var tag = Guid.NewGuid().ToString("N");
        for (var i = 0; i < 3; i++)
            await CreateKeywordAsync(value: $"List-{tag}-{i}");

        var list = await ListKeywordsAsync();

        Assert.True(list.Total >= 3);
        var taggedItems = list.Items.Where(k => k.Value.Contains(tag)).ToList();
        Assert.Equal(3, taggedItems.Count);
    }

    [Fact]
    public async Task Update_Value_UpdatedInDetail()
    {
        var id = await CreateKeywordAsync(value: $"OrigKw-{Guid.NewGuid():N}");
        var newValue = $"UpdatedKw-{Guid.NewGuid():N}";

        var payload = new KeywordManagementDto { Value = newValue };
        var response = await Client.PutAsJsonAsync($"/api/keywords/{id}", payload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await GetKeywordAsync(id);
        Assert.Equal(newValue, detail.Value);
    }

    [Fact]
    public async Task Update_DuplicateValue_ReturnsError()
    {
        var value1 = $"KwA-{Guid.NewGuid():N}";
        var value2 = $"KwB-{Guid.NewGuid():N}";
        await CreateKeywordAsync(value: value1);
        var id2 = await CreateKeywordAsync(value: value2);

        // Try to rename kw2 to kw1's value
        var payload = new KeywordManagementDto { Value = value1 };
        var response = await Client.PutAsJsonAsync($"/api/keywords/{id2}", payload);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        var payload = new KeywordManagementDto { Value = "Ghost" };

        var response = await Client.PutAsJsonAsync("/api/keywords/999999", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingKeyword_RemovesIt()
    {
        var id = await CreateKeywordAsync(value: $"KwDel-{Guid.NewGuid():N}");

        var deleteResponse = await Client.DeleteAsync($"/api/keywords/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/keywords/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync("/api/keywords/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_KeywordLinkedToPublication_UnlinksButKeepsPublication()
    {
        // Arrange — create a keyword, then a publication using it
        var kwValue = $"LinkedKw-{Guid.NewGuid():N}";
        var kwId = await CreateKeywordAsync(value: kwValue);

        var pubId = await CreatePublicationAsync(
            title: $"PubForKwDel-{Guid.NewGuid():N}",
            keywords: kwValue);

        // Act — delete the keyword via the keywords API
        var deleteResponse = await Client.DeleteAsync($"/api/keywords/{kwId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Assert — publication still exists but keyword is removed
        var updatedPub = await GetPublicationAsync(pubId);
        Assert.True(
            string.IsNullOrEmpty(updatedPub.Keywords) || !updatedPub.Keywords.Contains(kwValue));
    }

    [Fact]
    public async Task GetById_KeywordWithPublications_ReturnsCorrectCount()
    {
        // Arrange — create a unique keyword via two publications
        var kwValue = $"CountKw-{Guid.NewGuid():N}";
        await CreatePublicationAsync(
            title: $"PubKwCount1-{Guid.NewGuid():N}",
            keywords: kwValue);
        await CreatePublicationAsync(
            title: $"PubKwCount2-{Guid.NewGuid():N}",
            keywords: kwValue);

        // Find the keyword's ID
        var list = await ListKeywordsAsync();
        var kw = list.Items.First(k => k.Value == kwValue);

        // Act
        var detail = await GetKeywordAsync(kw.Id);

        // Assert
        Assert.Equal(2, detail.PublicationCount);
    }
}
