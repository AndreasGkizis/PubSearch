using ResearchPublications.Application.DTOs;

namespace ResearchPublications.Application.Interfaces;

public interface ISearchService
{
    Task<(IEnumerable<SearchResultDto> Items, int TotalCount)> SearchAsync(string query, SearchFilters filters, int page, int pageSize);
}
