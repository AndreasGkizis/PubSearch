using System.Net;
using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public class LanguageCrudTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_ValidLanguage_ReturnsCreatedWithId()
    {
        var payload = new LanguageManagementDto { Value = $"LangCreate-{Guid.NewGuid():N}" };

        var response = await Client.PostAsJsonAsync("/api/languages", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        Assert.True(result!.Id > 0);
    }

    [Fact]
    public async Task GetById_ExistingLanguage_ReturnsDetail()
    {
        var value = $"LangGet-{Guid.NewGuid():N}";
        var id = await CreateLanguageAsync(value: value);

        var detail = await GetLanguageAsync(id);

        Assert.Equal(value, detail.Value);
        Assert.Equal(0, detail.PublicationCount);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResults()
    {
        var tag = Guid.NewGuid().ToString("N");
        for (var i = 0; i < 3; i++)
            await CreateLanguageAsync(value: $"List-{tag}-{i}");

        var list = await ListLanguagesAsync();

        Assert.True(list.Total >= 3);
        var taggedItems = list.Items.Where(l => l.Value.Contains(tag)).ToList();
        Assert.Equal(3, taggedItems.Count);
    }

    [Fact]
    public async Task Update_Value_UpdatedInDetail()
    {
        var id = await CreateLanguageAsync(value: $"OrigLang-{Guid.NewGuid():N}");
        var newValue = $"UpdatedLang-{Guid.NewGuid():N}";

        var payload = new LanguageManagementDto { Value = newValue };
        var response = await Client.PutAsJsonAsync($"/api/languages/{id}", payload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await GetLanguageAsync(id);
        Assert.Equal(newValue, detail.Value);
    }

    [Fact]
    public async Task Delete_ExistingLanguage_RemovesIt()
    {
        var id = await CreateLanguageAsync(value: $"LangDel-{Guid.NewGuid():N}");

        var deleteResponse = await Client.DeleteAsync($"/api/languages/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/languages/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}

