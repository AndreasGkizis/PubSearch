using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Exceptions;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Application.Services;

public class AuthorService(IAuthorRepository repository, CacheService cacheService)
{
    public async Task<(IEnumerable<AuthorManagementDto> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search = null)
    {
        var (items, total) = await repository.GetAllAsync(page, pageSize, search);
        return (items.Select(ToDto), total);
    }

    public async Task<AuthorManagementDto> GetByIdAsync(int id)
    {
        var author = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Author {id} was not found.");
        return ToDto(author);
    }

    public async Task<int> CreateAsync(AuthorManagementDto dto)
    {
        var entity = FromDto(dto);
        var id = await repository.CreateAsync(entity);
        await cacheService.RefreshAuthorFilterOptionsAsync();
        return id;
    }

    public async Task UpdateAsync(int id, AuthorManagementDto dto)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Author {id} was not found.");

        var entity = FromDto(dto);
        entity.Id = id;
        await repository.UpdateAsync(entity);
        await cacheService.RefreshAuthorFilterOptionsAsync();
    }

    public async Task DeleteAsync(int id)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Author {id} was not found.");
        await repository.DeleteAsync(id);
        await cacheService.RefreshAuthorFilterOptionsAsync();
    }

    public async Task<IEnumerable<AuthorManagementDto>> SearchAsync(string query, int limit)
    {
        var items = await repository.SearchAsync(query, limit);
        return items.Select(ToDto);
    }

    private static AuthorManagementDto ToDto(Author a) => new()
    {
        Id = a.Id,
        FirstName = a.FirstName,
        MiddleName = a.MiddleName,
        LastName = a.LastName,
        Email = a.Email,
        PublicationCount = a.PublicationCount
    };

    private static Author FromDto(AuthorManagementDto dto) => new()
    {
        FirstName = dto.FirstName,
        MiddleName = dto.MiddleName,
        LastName = dto.LastName,
        Email = dto.Email
    };
}
