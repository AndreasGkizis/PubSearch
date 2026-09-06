using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using ResearchPublications.Application.Services;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.UnitTests.Support;

internal sealed class ServiceTestContext : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

    public IAuthorRepository Authors { get; } = Substitute.For<IAuthorRepository>();
    public IKeywordRepository Keywords { get; } = Substitute.For<IKeywordRepository>();
    public ILanguageRepository Languages { get; } = Substitute.For<ILanguageRepository>();
    public IPublicationTypeRepository PublicationTypes { get; } = Substitute.For<IPublicationTypeRepository>();
    public CacheService CacheService { get; }

    public ServiceTestContext()
    {
        Authors.GetFilterOptionsAsync().Returns(Task.FromResult<IEnumerable<(string Name, int Count)>>([]));
        Keywords.GetFilterOptionsAsync().Returns(Task.FromResult<IEnumerable<(string Name, int Count)>>([]));
        Languages.GetFilterOptionsAsync().Returns(Task.FromResult<IEnumerable<(string Name, int Count)>>([]));
        PublicationTypes.GetFilterOptionsAsync().Returns(Task.FromResult<IEnumerable<(string Name, int Count)>>([]));

        CacheService = new CacheService(_memoryCache, Authors, Keywords, Languages, PublicationTypes);
    }

    public void Dispose() => _memoryCache.Dispose();
}
