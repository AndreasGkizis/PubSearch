using System.Net;
using System.Net.Http.Json;
using ResearchPublications.Application.DTOs;
using ResearchPublications.IntegrationTests.Fixtures;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public class PublicationEditTests(PubSearchApiFactory factory) : IntegrationTestBase(factory)
{
    // ── Scalar field editing ───────────────────────────────────────────────

    [Fact]
    public async Task Update_ScalarFields_AllFieldsUpdated()
    {
        // Arrange
        var authorName = $"ScalarAuthor-{Guid.NewGuid():N}";
        var id = await CreatePublicationAsync(
            title: "Original Title",
            year: 2020,
            authors: [new AuthorDto { FullName = authorName }]);

        // Act
        var payload = new PublicationDetailDto
        {
            Title         = "Updated Title",
            Year          = 2025,
            DOI           = "10.9999/updated",
            Abstract      = "New abstract",
            Body          = "New body text",
            CitationCount = 42,
            Authors       = [new AuthorDto { FullName = authorName }]
        };
        var response = await Client.PutAsJsonAsync($"/api/publications/{id}", payload);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await GetPublicationAsync(id);
        Assert.Equal("Updated Title", detail.Title);
        Assert.Equal(2025, detail.Year);
        Assert.Equal("10.9999/updated", detail.DOI);
        Assert.Equal("New abstract", detail.Abstract);
        Assert.Equal("New body text", detail.Body);
        Assert.Equal(42, detail.CitationCount);
    }

    [Fact]
    public async Task Update_NonExistentPublication_ReturnsNotFound()
    {
        var payload = new PublicationDetailDto
        {
            Title   = "Ghost",
            Authors = [new AuthorDto { FullName = "Nobody" }]
        };

        var response = await Client.PutAsJsonAsync("/api/publications/999999", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Keyword editing ────────────────────────────────────────────────────

    [Fact]
    public async Task Update_AddKeywords_KeywordsAppearInDetail()
    {
        // Arrange — create without keywords
        var id = await CreatePublicationAsync(title: $"AddKw-{Guid.NewGuid():N}");

        // Act — add keywords
        var payload = new PublicationDetailDto
        {
            Title    = "AddKw-Updated",
            Keywords = "Machine Learning, Neural Networks",
            Authors  = [new AuthorDto { FullName = "KW Author" }]
        };
        await Client.PutAsJsonAsync($"/api/publications/{id}", payload);

        // Assert
        var detail = await GetPublicationAsync(id);
        Assert.Contains("Machine Learning", detail.Keywords!);
        Assert.Contains("Neural Networks", detail.Keywords!);
    }

    [Fact]
    public async Task Update_RemoveKeywords_KeywordsCleared()
    {
        // Arrange — create with keywords
        var id = await CreatePublicationAsync(
            title: $"RemoveKw-{Guid.NewGuid():N}",
            keywords: "Obsolete Keyword");

        // Act — update with no keywords
        var payload = new PublicationDetailDto
        {
            Title   = "RemoveKw-Updated",
            Authors = [new AuthorDto { FullName = "KW Author" }]
        };
        await Client.PutAsJsonAsync($"/api/publications/{id}", payload);

        // Assert
        var detail = await GetPublicationAsync(id);
        Assert.Null(detail.Keywords);
    }

    [Fact]
    public async Task Update_ChangeKeywords_OldReplacedWithNew()
    {
        // Arrange
        var id = await CreatePublicationAsync(
            title: $"SwapKw-{Guid.NewGuid():N}",
            keywords: "OldKeyword");

        // Act
        var payload = new PublicationDetailDto
        {
            Title    = "SwapKw-Updated",
            Keywords = "NewKeywordA, NewKeywordB",
            Authors  = [new AuthorDto { FullName = "KW Author" }]
        };
        await Client.PutAsJsonAsync($"/api/publications/{id}", payload);

        // Assert
        var detail = await GetPublicationAsync(id);
        Assert.DoesNotContain("OldKeyword", detail.Keywords ?? "");
        Assert.Contains("NewKeywordA", detail.Keywords!);
        Assert.Contains("NewKeywordB", detail.Keywords!);
    }

    [Fact]
    public async Task Update_SharedKeyword_NoDuplicateKeywordRows()
    {
        // Arrange — unique keyword shared by two publications
        var keyword = $"SharedKw-{Guid.NewGuid():N}";
        await CreatePublicationAsync(title: $"KwDedup-1-{Guid.NewGuid():N}", keywords: keyword);
        await CreatePublicationAsync(title: $"KwDedup-2-{Guid.NewGuid():N}", keywords: keyword);

        // Act
        var allKeywords = await GetAllKeywordValuesAsync();

        // Assert — keyword appears exactly once in the Keywords table
        Assert.Single(allKeywords, k => k == keyword);
    }

    // ── Author editing ─────────────────────────────────────────────────────

    [Fact]
    public async Task Update_SameAuthorWithoutId_NoDuplicateAuthorRow()
    {
        // Arrange — create a publication with a unique author
        var authorName = $"DedupAuthor-{Guid.NewGuid():N}";
        var id = await CreatePublicationAsync(
            title: $"AuthorDedup-{Guid.NewGuid():N}",
            authors: [new AuthorDto { FullName = authorName }]);

        // Act — edit the publication, sending the author without an ID (id = 0).
        //        This simulates what the admin UI does on each edit round-trip.
        var payload = new PublicationDetailDto
        {
            Title   = "AuthorDedup-Updated",
            Authors = [new AuthorDto { FullName = authorName }] // id defaults to 0
        };
        await Client.PutAsJsonAsync($"/api/publications/{id}", payload);

        // Assert — the author name must appear only once in the Authors table
        var allAuthors = await GetAllAuthorNamesAsync();
        Assert.Single(allAuthors, a => a == authorName);
    }

    [Fact]
    public async Task Update_AddNewAuthor_AuthorAppearsInDetail()
    {
        // Arrange
        var originalAuthor = $"OrigAuth-{Guid.NewGuid():N}";
        var id = await CreatePublicationAsync(
            title: $"AddAuth-{Guid.NewGuid():N}",
            authors: [new AuthorDto { FullName = originalAuthor }]);

        // Act — add a second author
        var newAuthor = $"NewAuth-{Guid.NewGuid():N}";
        var payload = new PublicationDetailDto
        {
            Title   = "AddAuth-Updated",
            Authors =
            [
                new AuthorDto { FullName = originalAuthor },
                new AuthorDto { FullName = newAuthor }
            ]
        };
        await Client.PutAsJsonAsync($"/api/publications/{id}", payload);

        // Assert
        var detail = await GetPublicationAsync(id);
        Assert.Equal(2, detail.Authors.Count);
        Assert.Contains(detail.Authors, a => a.FullName == originalAuthor);
        Assert.Contains(detail.Authors, a => a.FullName == newAuthor);
    }

    [Fact]
    public async Task Update_RemoveAuthor_AuthorRemovedFromPublication()
    {
        // Arrange
        var keep   = $"KeepAuth-{Guid.NewGuid():N}";
        var remove = $"RemoveAuth-{Guid.NewGuid():N}";
        var id = await CreatePublicationAsync(
            title: $"RemAuth-{Guid.NewGuid():N}",
            authors:
            [
                new AuthorDto { FullName = keep },
                new AuthorDto { FullName = remove }
            ]);

        // Act — update with only the first author
        var payload = new PublicationDetailDto
        {
            Title   = "RemAuth-Updated",
            Authors = [new AuthorDto { FullName = keep }]
        };
        await Client.PutAsJsonAsync($"/api/publications/{id}", payload);

        // Assert
        var detail = await GetPublicationAsync(id);
        Assert.Single(detail.Authors);
        Assert.Equal(keep, detail.Authors[0].FullName);
    }

    [Fact]
    public async Task Create_SharedAuthorAcrossPublications_NoDuplicateAuthorRow()
    {
        // Arrange — two publications created independently with the same author name
        var sharedAuthor = $"SharedAuth-{Guid.NewGuid():N}";
        await CreatePublicationAsync(
            title: $"SharedAuth-1-{Guid.NewGuid():N}",
            authors: [new AuthorDto { FullName = sharedAuthor }]);

        await CreatePublicationAsync(
            title: $"SharedAuth-2-{Guid.NewGuid():N}",
            authors: [new AuthorDto { FullName = sharedAuthor }]);

        // Assert — the author name must appear only once in the Authors table
        var allAuthors = await GetAllAuthorNamesAsync();
        Assert.Single(allAuthors, a => a == sharedAuthor);
    }
}
