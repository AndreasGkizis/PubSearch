using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Domain.Interfaces;

public interface ILanguageRepository
{
    Task<(IEnumerable<Language> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Language?> GetByIdAsync(int id);
    Task<Language?> GetByValueAsync(string value);
    Task<int> CreateAsync(Language language);
    Task UpdateAsync(Language language);
    Task DeleteAsync(int id);
    Task<IEnumerable<(string Name, int Count)>> GetFilterOptionsAsync();
    Task<IEnumerable<Language>> SearchAsync(string query, int limit);
}
