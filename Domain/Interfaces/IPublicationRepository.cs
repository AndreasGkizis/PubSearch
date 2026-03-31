using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Domain.Interfaces;

public interface IPublicationRepository
{
    Task<Publication?> GetByIdAsync(int id);
    Task<(IEnumerable<Publication> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize,
        int? yearFrom = null, int? yearTo = null,
        IReadOnlyList<string>? authors = null,
        IReadOnlyList<string>? keywords = null);
    Task<int> CreateAsync(Publication publication);
    Task UpdateAsync(Publication publication);
    Task DeleteAsync(int id);
    Task<IEnumerable<(string Name, int Count)>> GetAllAuthorsAsync();
    Task<IEnumerable<(string Name, int Count)>> GetAllKeywordsAsync();
}
