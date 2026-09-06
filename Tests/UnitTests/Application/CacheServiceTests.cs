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
}
