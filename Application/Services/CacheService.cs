using Microsoft.Extensions.Caching.Memory;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Application.Services;

public class CacheService(IMemoryCache cache, IAuthorRepository authorRepository, IKeywordRepository keywordRepository, ILanguageRepository languageRepository, IPublicationTypeRepository publicationTypeRepository)
{
    private const string AuthorFilterOptionsCacheKey = "filter-options:authors";
    private const string KeywordFilterOptionsCacheKey = "filter-options:keywords";
    private const string LanguageFilterOptionsCacheKey = "filter-options:languages";
    private const string PublicationTypeFilterOptionsCacheKey = "filter-options:publication-types";

    public Task<IEnumerable<FilterOptionDto>> GetAuthorFilterOptionsAsync() =>
        GetFilterOptionsAsync(AuthorFilterOptionsCacheKey, authorRepository.GetFilterOptionsAsync);

    public Task<IEnumerable<FilterOptionDto>> GetKeywordFilterOptionsAsync() =>
        GetFilterOptionsAsync(KeywordFilterOptionsCacheKey, keywordRepository.GetFilterOptionsAsync);

    public Task<IEnumerable<FilterOptionDto>> GetLanguageFilterOptionsAsync() =>
        GetFilterOptionsAsync(LanguageFilterOptionsCacheKey, languageRepository.GetFilterOptionsAsync);

    public Task<IEnumerable<FilterOptionDto>> GetPublicationTypeFilterOptionsAsync() =>
        GetFilterOptionsAsync(PublicationTypeFilterOptionsCacheKey, publicationTypeRepository.GetFilterOptionsAsync);

    public void InvalidateAuthorFilterOptions() => cache.Remove(AuthorFilterOptionsCacheKey);
    public void InvalidateKeywordFilterOptions() => cache.Remove(KeywordFilterOptionsCacheKey);
    public void InvalidateLanguageFilterOptions() => cache.Remove(LanguageFilterOptionsCacheKey);
    public void InvalidatePublicationTypeFilterOptions() => cache.Remove(PublicationTypeFilterOptionsCacheKey);

    public void InvalidateAllFilterOptions()
    {
        InvalidateAuthorFilterOptions();
        InvalidateKeywordFilterOptions();
        InvalidateLanguageFilterOptions();
        InvalidatePublicationTypeFilterOptions();
    }

    private async Task<IEnumerable<FilterOptionDto>> GetFilterOptionsAsync(
        string key, Func<Task<IEnumerable<(string Name, int Count)>>> load) =>
        await cache.GetOrCreateAsync(key, async _ =>
            (await load()).Select(item => new FilterOptionDto(item.Name, item.Count)).ToList()) ?? [];
}
