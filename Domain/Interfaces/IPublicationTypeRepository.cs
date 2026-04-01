using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Domain.Interfaces;

public interface IPublicationTypeRepository
{
    Task<(IEnumerable<PublicationType> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search = null);
    Task<PublicationType?> GetByIdAsync(int id);
    Task<PublicationType?> GetByValueAsync(string value);
    Task<int> CreateAsync(PublicationType publicationType);
    Task UpdateAsync(PublicationType publicationType);
    Task DeleteAsync(int id);
    Task<IEnumerable<(string Name, int Count)>> GetFilterOptionsAsync();
    Task<IEnumerable<PublicationType>> SearchAsync(string query, int limit);
}
