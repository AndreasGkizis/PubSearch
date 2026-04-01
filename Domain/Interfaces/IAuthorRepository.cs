using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Domain.Interfaces;

public interface IAuthorRepository
{
    Task<(IEnumerable<Author> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search = null);
    Task<Author?> GetByIdAsync(int id);
    Task<int> CreateAsync(Author author);
    Task UpdateAsync(Author author);
    Task DeleteAsync(int id);
    Task<IEnumerable<(string Name, int Count)>> GetFilterOptionsAsync();
    Task<IEnumerable<Author>> SearchAsync(string query, int limit);
}
