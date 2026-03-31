using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Domain.Interfaces;

public interface IAuthorRepository
{
    Task<(IEnumerable<Author> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Author?> GetByIdAsync(int id);
    Task<int> CreateAsync(Author author);
    Task UpdateAsync(Author author);
    Task DeleteAsync(int id);
}
