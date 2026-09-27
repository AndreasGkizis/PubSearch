using ResearchPublications.Application.DTOs;

namespace ResearchPublications.Application.Interfaces;

public interface IOcrSearchService
{
    Task<(IEnumerable<OcrSearchResultDto> Items, int TotalCount)> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
