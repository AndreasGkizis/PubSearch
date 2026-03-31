using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Exceptions;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Application.Services;

public class AuthorService(IAuthorRepository repository)
{
    public async Task<(IEnumerable<AuthorManagementDto> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var (items, total) = await repository.GetAllAsync(page, pageSize);
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
        return await repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(int id, AuthorManagementDto dto)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Author {id} was not found.");

        var entity = FromDto(dto);
        entity.Id = id;
        await repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Author {id} was not found.");
        await repository.DeleteAsync(id);
    }

    private static AuthorManagementDto ToDto(Author a) => new()
    {
        Id = a.Id,
        FullName = a.FullName,
        FirstName = a.FirstName,
        LastName = a.LastName,
        Email = a.Email,
        PublicationCount = a.Publications.Count
    };

    private static Author FromDto(AuthorManagementDto dto) => new()
    {
        FullName = dto.FullName,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email
    };
}
