using System.Net;
using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public class PublicationCrudTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_ValidPublication_ReturnsCreatedWithId()
    {
        // Arrange
        var payload = new PublicationDetailDto
        {
            Title    = $"CRUD-Create-{Guid.NewGuid():N}",
            Year     = 2024,
            Keywords = "Testing",
            Authors  = [new AuthorDto { FirstName = "Test", LastName = "Author" }]
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/publications", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        Assert.True(result!.Id > 0);
    }

    [Fact]
    public async Task GetById_ExistingPublication_ReturnsFullDetail()
    {
        // Arrange
        var title = $"CRUD-Get-{Guid.NewGuid():N}";
        var id = await CreatePublicationAsync(
            title: title,
            year: 2023,
            @abstract: "A detailed abstract about testing",
            doi: "10.1234/test.get",
            keywords: "AI, ML",
            authors: [new AuthorDto { FirstName = "Alice", LastName = "Get", Email = "alice@test.com" }]);

        // Act
        var detail = await GetPublicationAsync(id);

        // Assert
        Assert.Equal(title, detail.Title);
        Assert.Equal(2023, detail.Year);
        Assert.Equal("A detailed abstract about testing", detail.Abstract);
        Assert.Equal("10.1234/test.get", detail.DOI);
        Assert.Contains("AI", detail.Keywords!);
        Assert.Contains("ML", detail.Keywords!);
        Assert.Single(detail.Authors);
        Assert.Equal("Alice Get", detail.Authors[0].FullName);
        Assert.Equal("alice@test.com", detail.Authors[0].Email);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/publications/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResults()
    {
        // Arrange — create 3 publications with a unique tag to identify them
        var tag = Guid.NewGuid().ToString("N");
        for (var i = 0; i < 3; i++)
            await CreatePublicationAsync(title: $"List-{tag}-{i}");

        // Act
        var list = await ListPublicationsAsync();

        // Assert
        Assert.True(list.Total >= 3);
        var taggedItems = list.Items.Where(p => p.Title.Contains(tag)).ToList();
        Assert.Equal(3, taggedItems.Count);
    }

    [Fact]
    public async Task Delete_ExistingPublication_RemovesIt()
    {
        // Arrange
        var id = await CreatePublicationAsync(title: $"CRUD-Delete-{Guid.NewGuid():N}");

        // Act
        var deleteResponse = await Client.DeleteAsync($"/api/publications/{id}");

        // Assert — delete succeeds
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Assert — publication is gone
        var getResponse = await Client.GetAsync($"/api/publications/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
