using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using Typesense;

namespace ResearchPublications.Infrastructure.Search;

internal sealed class TypesenseOcrSearchService(ITypesenseClient typesense) : IOcrSearchService
{
    private const string CollectionName = "publications";

    public async Task<(IEnumerable<OcrSearchResultDto> Items, int TotalCount)> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var parameters = new SearchParameters(query, "ocr_text")
        {
            Page = page,
            PerPage = pageSize,
            HighlightFields = "ocr_text",
            HighlightStartTag = "<mark>",
            HighlightEndTag = "</mark>",
            HighlightAffixNumberOfTokens = 20,
            NumberOfTypos = "2",
            ExhaustiveSearch = false
        };

        var result = await typesense.Search<PublicationDocument>(CollectionName, parameters, cancellationToken);
        var items = (result.Hits ?? []).Select(hit =>
        {
            var document = hit.Document;
            var highlight = hit.Highlights?.FirstOrDefault(item => item.Field == "ocr_text")?.Snippet;
            var fallback = document.OcrText is { Length: > 240 }
                ? document.OcrText[..240] + "\u2026"
                : document.OcrText;

            return new OcrSearchResultDto
            {
                Id = int.Parse(document.Id),
                Title = document.Title,
                Authors = document.Authors.ToList(),
                Year = document.Year == 0 ? null : document.Year,
                OcrSnippet = highlight ?? fallback,
                PdfFileName = string.IsNullOrWhiteSpace(document.PdfFileName) ? null : document.PdfFileName
            };
        }).ToList();

        return (items, result.Found);
    }
}
