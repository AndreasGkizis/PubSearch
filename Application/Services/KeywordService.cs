using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Exceptions;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Application.Services;

public class KeywordService(IKeywordRepository repository)
{
    public async Task<(IEnumerable<KeywordManagementDto> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var (items, total) = await repository.GetAllAsync(page, pageSize);
        return (items.Select(ToDto), total);
    }

    public async Task<KeywordManagementDto> GetByIdAsync(int id)
    {
        var keyword = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Keyword {id} was not found.");
        return ToDto(keyword);
    }

    public async Task<int> CreateAsync(KeywordManagementDto dto)
    {
        var duplicate = await repository.GetByValueAsync(dto.Value);
        if (duplicate is not null)
            throw new InvalidOperationException($"A keyword with value '{dto.Value}' already exists.");

        var entity = new Keyword { Value = dto.Value };
        return await repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(int id, KeywordManagementDto dto)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Keyword {id} was not found.");

        var duplicate = await repository.GetByValueAsync(dto.Value);
        if (duplicate is not null && duplicate.Id != id)
            throw new InvalidOperationException($"A keyword with value '{dto.Value}' already exists.");

        var entity = new Keyword { Id = id, Value = dto.Value };
        await repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Keyword {id} was not found.");
        await repository.DeleteAsync(id);
    }

    private static KeywordManagementDto ToDto(Keyword k) => new()
    {
        Id = k.Id,
        Value = k.Value,
        PublicationCount = k.PublicationCount
    };
}
