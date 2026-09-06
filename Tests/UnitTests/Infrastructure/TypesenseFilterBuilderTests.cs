using ResearchPublications.Application.DTOs;
using ResearchPublications.Infrastructure.Search;

namespace ResearchPublications.UnitTests.Infrastructure;

public sealed class TypesenseFilterBuilderTests
{
    [Fact]
    public void Build_NoFilters_ReturnsNoFilterExpression()
    {
        // Arrange
        var filters = new SearchFilters(null, null, null, null, null, null);

        // Act
        var expression = TypesenseFilterBuilder.Build(filters);

        // Assert
        Assert.Null(expression);
    }

    [Fact]
    public void Build_AllFilters_CreatesEscapedTypesenseExpression()
    {
        // Arrange
        var filters = new SearchFilters(
            1990,
            2020,
            ["Smith, Anna"],
            ["wall `painting`"],
            ["English"],
            ["Journal Article"]);

        // Act
        var expression = TypesenseFilterBuilder.Build(filters);

        // Assert
        Assert.Equal(
            "year:>=1990 && year:<=2020 && authors:[`Smith, Anna`] && keywords:[`wall \\`painting\\``] && languages:[`English`] && publication_types:[`Journal Article`]",
            expression);
    }
}
