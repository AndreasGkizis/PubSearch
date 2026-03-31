using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Exceptions;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Application.Services;

public class LanguageService(ILanguageRepository repository, CacheService cacheService)
{
    public async Task<(IEnumerable<LanguageManagementDto> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var (items, total) = await repository.GetAllAsync(page, pageSize);
        return (items.Select(ToDto), total);
    }

    public async Task<LanguageManagementDto> GetByIdAsync(int id)
    {
        var language = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Language {id} was not found.");
        return ToDto(language);
    }

    public async Task<int> CreateAsync(LanguageManagementDto dto)
    {
        var duplicate = await repository.GetByValueAsync(dto.Value);
        if (duplicate is not null)
            throw new InvalidOperationException($"A language with value '{dto.Value}' already exists.");

        var entity = new Language { Value = dto.Value };
        var id = await repository.CreateAsync(entity);
        await cacheService.RefreshLanguageFilterOptionsAsync();
        return id;
    }

    public async Task UpdateAsync(int id, LanguageManagementDto dto)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Language {id} was not found.");

        var duplicate = await repository.GetByValueAsync(dto.Value);
        if (duplicate is not null && duplicate.Id != id)
            throw new InvalidOperationException($"A language with value '{dto.Value}' already exists.");

        var entity = new Language { Id = id, Value = dto.Value };
        await repository.UpdateAsync(entity);
        await cacheService.RefreshLanguageFilterOptionsAsync();
    }

    public async Task DeleteAsync(int id)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Language {id} was not found.");
        await repository.DeleteAsync(id);
        await cacheService.RefreshLanguageFilterOptionsAsync();
    }

    public async Task<IEnumerable<LanguageManagementDto>> SearchAsync(string query, int limit)
    {
        var items = await repository.SearchAsync(query, limit);
        return items.Select(ToDto);
    }

    private static LanguageManagementDto ToDto(Language l) => new()
    {
        Id = l.Id,
        Value = l.Value,
        PublicationCount = l.PublicationCount
    };
}
