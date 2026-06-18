using System.Net;
using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public class PublicationTypeCrudTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_ValidPublicationType_ReturnsCreatedWithId()
    {
        var payload = new PublicationTypeManagementDto { Value = $"TypeCreate-{Guid.NewGuid():N}" };

        var response = await Client.PostAsJsonAsync("/api/publication-types", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        Assert.True(result!.Id > 0);
    }

    [Fact]
    public async Task GetById_ExistingPublicationType_ReturnsDetail()
    {
        var value = $"TypeGet-{Guid.NewGuid():N}";
        var id = await CreatePublicationTypeAsync(value: value);

        var detail = await GetPublicationTypeAsync(id);

        Assert.Equal(value, detail.Value);
        Assert.Equal(0, detail.PublicationCount);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResults()
    {
        var tag = Guid.NewGuid().ToString("N");
        for (var i = 0; i < 3; i++)
            await CreatePublicationTypeAsync(value: $"List-{tag}-{i}");

        var list = await ListPublicationTypesAsync();

        Assert.True(list.Total >= 3);
        var taggedItems = list.Items.Where(t => t.Value.Contains(tag)).ToList();
        Assert.Equal(3, taggedItems.Count);
    }

    [Fact]
    public async Task Update_Value_UpdatedInDetail()
    {
        var id = await CreatePublicationTypeAsync(value: $"OrigType-{Guid.NewGuid():N}");
        var newValue = $"UpdatedType-{Guid.NewGuid():N}";

        var payload = new PublicationTypeManagementDto { Value = newValue };
        var response = await Client.PutAsJsonAsync($"/api/publication-types/{id}", payload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await GetPublicationTypeAsync(id);
        Assert.Equal(newValue, detail.Value);
    }

    [Fact]
    public async Task Delete_ExistingPublicationType_RemovesIt()
    {
        var id = await CreatePublicationTypeAsync(value: $"TypeDel-{Guid.NewGuid():N}");

        var deleteResponse = await Client.DeleteAsync($"/api/publication-types/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/publication-types/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}

