using System.Net;
using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public class AuthorCrudTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_ValidAuthor_ReturnsCreatedWithId()
    {
        var payload = new AuthorManagementDto
        {
            FirstName = $"Create-{Guid.NewGuid():N}",
            LastName  = "Doe",
            Email     = "john@test.com"
        };

        var response = await Client.PostAsJsonAsync("/api/authors", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
        Assert.True(result!.Id > 0);
    }

    [Fact]
    public async Task GetById_ExistingAuthor_ReturnsDetail()
    {
        var id = await CreateAuthorAsync(firstName: "Alice", lastName: "Smith", email: "alice@test.com");

        var detail = await GetAuthorAsync(id);

        Assert.Equal("Alice", detail.FirstName);
        Assert.Equal("Smith", detail.LastName);
        Assert.Equal("alice@test.com", detail.Email);
        Assert.Equal(0, detail.PublicationCount);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var response = await Client.GetAsync("/api/authors/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResults()
    {
        // Arrange — create 3 authors so we know there are at least some
        for (var i = 0; i < 3; i++)
            await CreateAuthorAsync();

        // Act
        var list = await ListAuthorsAsync(page: 1, pageSize: 5);

        // Assert — total includes at least our 3, page is bounded
        Assert.True(list.Total >= 3);
        Assert.True(list.Items.Count > 0 && list.Items.Count <= 5);
    }

    [Fact]
    public async Task Update_ScalarFields_AllFieldsUpdated()
    {
        var id = await CreateAuthorAsync(firstName: "Orig", lastName: "Author");

        var payload = new AuthorManagementDto
        {
            FirstName = "New",
            LastName  = "Author",
            Email     = "new@test.com"
        };
        var response = await Client.PutAsJsonAsync($"/api/authors/{id}", payload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await GetAuthorAsync(id);
        Assert.Equal("New", detail.FirstName);
        Assert.Equal("Author", detail.LastName);
        Assert.Equal("new@test.com", detail.Email);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        var payload = new AuthorManagementDto { FirstName = "Ghost", LastName = "Author" };

        var response = await Client.PutAsJsonAsync("/api/authors/999999", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingAuthor_RemovesIt()
    {
        var id = await CreateAuthorAsync(firstName: $"Delete-{Guid.NewGuid():N}", lastName: "Test");

        var deleteResponse = await Client.DeleteAsync($"/api/authors/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/authors/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync("/api/authors/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AuthorLinkedToPublication_UnlinksButKeepsPublication()
    {
        // Arrange — create publication with a unique author
        var authorFirstName = $"LinkedAuth-{Guid.NewGuid():N}";
        var pubId = await CreatePublicationAsync(
            title: $"PubForAuthDel-{Guid.NewGuid():N}",
            authors: [new AuthorDto { FirstName = authorFirstName, LastName = "Test" }]);

        // Find the author's ID from the publication detail
        var pubDetail = await GetPublicationAsync(pubId);
        var authorId = pubDetail.Authors.First(a => a.FirstName == authorFirstName).Id;

        // Act — delete the author via the authors API
        var deleteResponse = await Client.DeleteAsync($"/api/authors/{authorId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Assert — publication still exists but author is removed
        var updatedPub = await GetPublicationAsync(pubId);
        Assert.DoesNotContain(updatedPub.Authors, a => a.FirstName == authorFirstName);
    }

    [Fact]
    public async Task GetById_AuthorWithPublications_ReturnsCorrectCount()
    {
        // Arrange — create two publications with the same unique author
        var authorFirstName = $"CountAuth-{Guid.NewGuid():N}";
        var pubId1 = await CreatePublicationAsync(
            title: $"PubCount1-{Guid.NewGuid():N}",
            authors: [new AuthorDto { FirstName = authorFirstName, LastName = "Test" }]);
        await CreatePublicationAsync(
            title: $"PubCount2-{Guid.NewGuid():N}",
            authors: [new AuthorDto { FirstName = authorFirstName, LastName = "Test" }]);

        // Find the author's ID from the first publication's detail
        var pubDetail = await GetPublicationAsync(pubId1);
        var authorId = pubDetail.Authors.First(a => a.FirstName == authorFirstName).Id;

        // Act
        var detail = await GetAuthorAsync(authorId);

        // Assert
        Assert.Equal(2, detail.PublicationCount);
    }
}
