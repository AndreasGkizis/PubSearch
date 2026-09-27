using NSubstitute;
using ResearchPublications.UnitTests.Support;

namespace ResearchPublications.UnitTests.Application;

public sealed class CacheServiceTests
{
    [Fact]
    public async Task GetKeywordFilterOptions_SecondRequest_UsesCachedValues()
    {
        // Arrange
        using var context = new ServiceTestContext();
        var name = "Ceramics";
        var count = 12;
        context.Keywords.GetFilterOptionsAsync()
            .Returns(Task.FromResult<IEnumerable<(string Name, int Count)>>([(name, count)]));

        // Act
        var first = await context.CacheService.GetKeywordFilterOptionsAsync();
        var second = await context.CacheService.GetKeywordFilterOptionsAsync();

        // Assert
        Assert.Equal(first, second);
        var option = Assert.Single(second);
        Assert.Equal(name, option.Name);
        Assert.Equal(count, option.Count);
        await context.Keywords.Received(1).GetFilterOptionsAsync();
    }

    [Fact]
    public async Task InvalidateAllFilterOptions_ReloadsOnlyWhenRequested()
    {
        // Arrange
        using var context = new ServiceTestContext();
        await context.CacheService.GetAuthorFilterOptionsAsync();
        await context.CacheService.GetKeywordFilterOptionsAsync();
        await context.CacheService.GetLanguageFilterOptionsAsync();
        await context.CacheService.GetPublicationTypeFilterOptionsAsync();
        context.Keywords.GetFilterOptionsAsync().Returns(
            Task.FromResult<IEnumerable<(string Name, int Count)>>([("Updated", 3)]));

        // Act
        context.CacheService.InvalidateAllFilterOptions();

        // Assert: invalidation performs no reads; the next request sees fresh data.
        await context.Authors.Received(1).GetFilterOptionsAsync();
        await context.Keywords.Received(1).GetFilterOptionsAsync();
        await context.Languages.Received(1).GetFilterOptionsAsync();
        await context.PublicationTypes.Received(1).GetFilterOptionsAsync();
        var fresh = await context.CacheService.GetKeywordFilterOptionsAsync();
        Assert.Equal("Updated", Assert.Single(fresh).Name);
        Assert.Equal(3, Assert.Single(fresh).Count);
        await context.CacheService.GetKeywordFilterOptionsAsync();
        await context.Keywords.Received(2).GetFilterOptionsAsync();
        await context.CacheService.GetAuthorFilterOptionsAsync();
        await context.CacheService.GetLanguageFilterOptionsAsync();
        await context.CacheService.GetPublicationTypeFilterOptionsAsync();
        await context.Authors.Received(2).GetFilterOptionsAsync();
        await context.Languages.Received(2).GetFilterOptionsAsync();
        await context.PublicationTypes.Received(2).GetFilterOptionsAsync();
    }

}
