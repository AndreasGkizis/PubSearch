using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.IntegrationTests.Fixtures;
using Typesense;
using Xunit;

namespace ResearchPublications.IntegrationTests;

[Collection("Integration")]
public sealed class FixtureIsolationTests(PubSearchApiFactory factory)
{
    [Fact]
    public void CrudFixture_UsesNoOpIndexWithoutDevelopmentTypesenseClient()
    {
        using var scope = factory.Services.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<ITypesenseClient>());
        Assert.IsType<NoOpEntitySearchIndex>(scope.ServiceProvider.GetRequiredService<IEntitySearchIndex>());
    }
}
