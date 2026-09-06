using NSubstitute;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Exceptions;
using ResearchPublications.Application.Services;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;
using ResearchPublications.UnitTests.Support;

namespace ResearchPublications.UnitTests.Application;

public sealed class PublicationServiceTests
{
    [Fact]
    public async Task Create_CommaSeparatedRelationships_MapsCleanValuesAndRefreshesFilters()
    {
        // Arrange
        using var context = new ServiceTestContext();
        var repository = Substitute.For<IPublicationRepository>();
        Publication? savedPublication = null;
        repository.CreateAsync(Arg.Do<Publication>(publication => savedPublication = publication)).Returns(42);
        var service = new PublicationService(repository, context.CacheService);
        var request = new PublicationDetailDto
        {
            Title = "Wall Painting Conservation",
            Keywords = " pigments, fresco ,, conservation ",
            Languages = " English, Greek ",
            PublicationTypes = " Article, Thesis ",
            Authors = [new AuthorDto { FirstName = "Anna", MiddleName = "M.", LastName = "Smith" }]
        };

        // Act
        var id = await service.CreateAsync(request);

        // Assert
        Assert.Equal(42, id);
        Assert.NotNull(savedPublication);
        Assert.Equal(["pigments", "fresco", "conservation"], savedPublication.Keywords.Select(item => item.Value));
        Assert.Equal(["English", "Greek"], savedPublication.Languages.Select(item => item.Value));
        Assert.Equal(["Article", "Thesis"], savedPublication.PublicationTypes.Select(item => item.Value));
        Assert.Equal("Anna", savedPublication.Authors.Single().FirstName);
        Assert.Equal("M.", savedPublication.Authors.Single().MiddleName);
        await context.Authors.Received(1).GetFilterOptionsAsync();
        await context.Keywords.Received(1).GetFilterOptionsAsync();
        await context.Languages.Received(1).GetFilterOptionsAsync();
        await context.PublicationTypes.Received(1).GetFilterOptionsAsync();
    }

    [Fact]
    public async Task GetSummaries_PublicationWithRelatedData_FormatsReadableSummary()
    {
        // Arrange
        using var context = new ServiceTestContext();
        var repository = Substitute.For<IPublicationRepository>();
        var publication = new Publication
        {
            Id = 7,
            Title = "Conservation Study",
            Abstract = new string('a', 201),
            Authors =
            [
                new Author { FirstName = "Ada", LastName = "Lovelace" },
                new Author { FirstName = "Grace", MiddleName = "B.", LastName = "Hopper" }
            ],
            Keywords = [new Keyword { Value = "stone" }, new Keyword { Value = "survey" }],
            Languages = [new Language { Value = "English" }],
            PublicationTypes = [new PublicationType { Value = "Article" }]
        };
        repository.GetAllAsync(1, 20, null, null, null, null, null, null)
            .Returns(Task.FromResult<(IEnumerable<Publication>, int)>(([publication], 1)));
        var service = new PublicationService(repository, context.CacheService);

        // Act
        var (items, total) = await service.GetSummariesAsync(1, 20);

        // Assert
        var summary = Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal(["Ada Lovelace", "Grace B. Hopper"], summary.Authors);
        Assert.Equal("stone, survey", summary.Keywords);
        Assert.Equal("English", summary.Languages);
        Assert.Equal("Article", summary.PublicationTypes);
        Assert.Equal(201, summary.AbstractSnippet!.Length);
        Assert.EndsWith("…", summary.AbstractSnippet);
    }

    [Fact]
    public async Task Update_MissingPublication_ThrowsWithoutWriting()
    {
        // Arrange
        using var context = new ServiceTestContext();
        var repository = Substitute.For<IPublicationRepository>();
        repository.GetByIdAsync(99).Returns(Task.FromResult<Publication?>(null));
        var service = new PublicationService(repository, context.CacheService);

        // Act
        var action = () => service.UpdateAsync(99, new PublicationDetailDto { Title = "Missing" });

        // Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(action);
        Assert.Equal("Publication 99 was not found.", exception.Message);
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Publication>());
    }
}
