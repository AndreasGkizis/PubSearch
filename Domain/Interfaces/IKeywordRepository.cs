using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Domain.Interfaces;

public interface IKeywordRepository
{
    Task<(IEnumerable<Keyword> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Keyword?> GetByIdAsync(int id);
    Task<Keyword?> GetByValueAsync(string value);
    Task<int> CreateAsync(Keyword keyword);
    Task UpdateAsync(Keyword keyword);
    Task DeleteAsync(int id);
}
