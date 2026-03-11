using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Domain.Interfaces;

public interface IPublicationRepository
{
    Task<Publication?> GetByIdAsync(int id);
    Task<(IEnumerable<Publication> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<int> CreateAsync(Publication publication);
    Task UpdateAsync(Publication publication);
    Task DeleteAsync(int id);
}
