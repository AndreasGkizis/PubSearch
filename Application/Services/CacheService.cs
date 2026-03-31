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

    public async Task<IEnumerable<FilterOptionDto>> GetAuthorFilterOptionsAsync()
    {
        if (cache.TryGetValue(AuthorFilterOptionsCacheKey, out List<FilterOptionDto>? cached) && cached is not null)
            return cached;

        return await RefreshAuthorFilterOptionsAsync();
    }

    public async Task<IEnumerable<FilterOptionDto>> GetKeywordFilterOptionsAsync()
    {
        if (cache.TryGetValue(KeywordFilterOptionsCacheKey, out List<FilterOptionDto>? cached) && cached is not null)
            return cached;

        return await RefreshKeywordFilterOptionsAsync();
    }

    public async Task<List<FilterOptionDto>> RefreshAuthorFilterOptionsAsync()
    {
        var items = await authorRepository.GetFilterOptionsAsync();
        var result = items.Select(x => new FilterOptionDto(x.Name, x.Count)).ToList();
        cache.Set(AuthorFilterOptionsCacheKey, result);
        return result;
    }

    public async Task<List<FilterOptionDto>> RefreshKeywordFilterOptionsAsync()
    {
        var items = await keywordRepository.GetFilterOptionsAsync();
        var result = items.Select(x => new FilterOptionDto(x.Name, x.Count)).ToList();
        cache.Set(KeywordFilterOptionsCacheKey, result);
        return result;
    }

    public async Task<IEnumerable<FilterOptionDto>> GetLanguageFilterOptionsAsync()
    {
        if (cache.TryGetValue(LanguageFilterOptionsCacheKey, out List<FilterOptionDto>? cached) && cached is not null)
            return cached;

        return await RefreshLanguageFilterOptionsAsync();
    }

    public async Task<IEnumerable<FilterOptionDto>> GetPublicationTypeFilterOptionsAsync()
    {
        if (cache.TryGetValue(PublicationTypeFilterOptionsCacheKey, out List<FilterOptionDto>? cached) && cached is not null)
            return cached;

        return await RefreshPublicationTypeFilterOptionsAsync();
    }

    public async Task<List<FilterOptionDto>> RefreshLanguageFilterOptionsAsync()
    {
        var items = await languageRepository.GetFilterOptionsAsync();
        var result = items.Select(x => new FilterOptionDto(x.Name, x.Count)).ToList();
        cache.Set(LanguageFilterOptionsCacheKey, result);
        return result;
    }

    public async Task<List<FilterOptionDto>> RefreshPublicationTypeFilterOptionsAsync()
    {
        var items = await publicationTypeRepository.GetFilterOptionsAsync();
        var result = items.Select(x => new FilterOptionDto(x.Name, x.Count)).ToList();
        cache.Set(PublicationTypeFilterOptionsCacheKey, result);
        return result;
    }
}
