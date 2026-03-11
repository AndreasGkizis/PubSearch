using ResearchPublications.Application.DTOs;

namespace ResearchPublications.Application.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<SearchResultDto>> SearchAsync(string query, SearchFilters filters);
}
