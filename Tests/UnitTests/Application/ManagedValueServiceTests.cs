using NSubstitute;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Services;
using ResearchPublications.Domain.Entities;
using ResearchPublications.UnitTests.Support;

namespace ResearchPublications.UnitTests.Application;

public sealed class ManagedValueServiceTests
{
    [Fact]
    public async Task CreateKeyword_DuplicateValue_RejectsTheDuplicate()
    {
        // Arrange
        using var context = new ServiceTestContext();
        context.Keywords.GetByValueAsync("Ceramics").Returns(new Keyword { Id = 4, Value = "Ceramics" });
        var service = new KeywordService(context.Keywords, context.CacheService);

        // Act
        var action = () => service.CreateAsync(new KeywordManagementDto { Value = "Ceramics" });

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("A keyword with value 'Ceramics' already exists.", exception.Message);
        await context.Keywords.DidNotReceive().CreateAsync(Arg.Any<Keyword>());
    }

    [Fact]
    public async Task UpdateLanguage_ValueBelongsToAnotherLanguage_RejectsTheDuplicate()
    {
        // Arrange
        using var context = new ServiceTestContext();
        context.Languages.GetByIdAsync(5).Returns(new Language { Id = 5, Value = "English" });
        context.Languages.GetByValueAsync("Greek").Returns(new Language { Id = 8, Value = "Greek" });
        var service = new LanguageService(context.Languages, context.CacheService);

        // Act
        var action = () => service.UpdateAsync(5, new LanguageManagementDto { Value = "Greek" });

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
        await context.Languages.DidNotReceive().UpdateAsync(Arg.Any<Language>());
    }

    [Fact]
    public async Task UpdatePublicationType_ValueBelongsToSameRecord_AllowsTheUpdate()
    {
        // Arrange
        using var context = new ServiceTestContext();
        context.PublicationTypes.GetByIdAsync(3).Returns(new PublicationType { Id = 3, Value = "Article" });
        context.PublicationTypes.GetByValueAsync("Article").Returns(new PublicationType { Id = 3, Value = "Article" });
        var service = new PublicationTypeService(context.PublicationTypes, context.CacheService);

        // Act
        await service.UpdateAsync(3, new PublicationTypeManagementDto { Value = "Article" });

        // Assert
        await context.PublicationTypes.Received(1).UpdateAsync(
            Arg.Is<PublicationType>(item => item.Id == 3 && item.Value == "Article"));
        await context.PublicationTypes.Received(1).GetFilterOptionsAsync();
    }
}
