using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Exceptions;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Application.Services;

public class PublicationTypeService(IPublicationTypeRepository repository, CacheService cacheService)
{
    public async Task<(IEnumerable<PublicationTypeManagementDto> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search = null)
    {
        var (items, total) = await repository.GetAllAsync(page, pageSize, search);
        return (items.Select(ToDto), total);
    }

    public async Task<PublicationTypeManagementDto> GetByIdAsync(int id)
    {
        var publicationType = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Publication type {id} was not found.");
        return ToDto(publicationType);
    }

    public async Task<int> CreateAsync(PublicationTypeManagementDto dto)
    {
        var duplicate = await repository.GetByValueAsync(dto.Value);
        if (duplicate is not null)
            throw new InvalidOperationException($"A publication type with value '{dto.Value}' already exists.");

        var entity = new PublicationType { Value = dto.Value };
        var id = await repository.CreateAsync(entity);
        await cacheService.RefreshPublicationTypeFilterOptionsAsync();
        return id;
    }

    public async Task UpdateAsync(int id, PublicationTypeManagementDto dto)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Publication type {id} was not found.");

        var duplicate = await repository.GetByValueAsync(dto.Value);
        if (duplicate is not null && duplicate.Id != id)
            throw new InvalidOperationException($"A publication type with value '{dto.Value}' already exists.");

        var entity = new PublicationType { Id = id, Value = dto.Value };
        await repository.UpdateAsync(entity);
        await cacheService.RefreshPublicationTypeFilterOptionsAsync();
    }

    public async Task DeleteAsync(int id)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Publication type {id} was not found.");
        await repository.DeleteAsync(id);
        await cacheService.RefreshPublicationTypeFilterOptionsAsync();
    }

    public async Task<IEnumerable<PublicationTypeManagementDto>> SearchAsync(string query, int limit)
    {
        var items = await repository.SearchAsync(query, limit);
        return items.Select(ToDto);
    }

    private static PublicationTypeManagementDto ToDto(PublicationType pt) => new()
    {
        Id = pt.Id,
        Value = pt.Value,
        PublicationCount = pt.PublicationCount
    };
}
