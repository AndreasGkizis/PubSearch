using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;
using Keyword = ResearchPublications.Domain.Entities.Keyword;
using Language = ResearchPublications.Domain.Entities.Language;
using PublicationType = ResearchPublications.Domain.Entities.PublicationType;

namespace ResearchPublications.Application.Services;

public class PublicationService(IPublicationRepository repository, CacheService cacheService, ITypesenseIndexingService indexingService)
{
    public async Task<(IEnumerable<PublicationSummaryDto> Items, int TotalCount)> GetSummariesAsync(
        int page, int pageSize, SearchFilters? filters = null)
    {
        var (items, total) = await repository.GetAllAsync(
            page, pageSize,
            filters?.YearFrom, filters?.YearTo,
            filters?.Authors, filters?.Keywords,
            filters?.Languages, filters?.PublicationTypes);
        return (items.Select(ToSummary), total);
    }

    public async Task<PublicationDetailDto> GetDetailAsync(int id)
    {
        var pub = await repository.GetByIdAsync(id)
            ?? throw new Exceptions.NotFoundException($"Publication {id} was not found.");
        return ToDetail(pub);
    }

    public async Task<int> CreateAsync(PublicationDetailDto dto)
    {
        var entity = FromDetail(dto);
        var id = await repository.CreateAsync(entity);
        await cacheService.RefreshAuthorFilterOptionsAsync();
        await cacheService.RefreshKeywordFilterOptionsAsync();
        await cacheService.RefreshLanguageFilterOptionsAsync();
        await cacheService.RefreshPublicationTypeFilterOptionsAsync();

        var created = await repository.GetByIdAsync(id);
        if (created is not null)
            await indexingService.IndexPublicationAsync(created);

        return id;
    }

    public async Task UpdateAsync(int id, PublicationDetailDto dto)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new Exceptions.NotFoundException($"Publication {id} was not found.");
        var entity = FromDetail(dto);
        entity.Id = id;
        await repository.UpdateAsync(entity);
        await cacheService.RefreshAuthorFilterOptionsAsync();
        await cacheService.RefreshKeywordFilterOptionsAsync();
        await cacheService.RefreshLanguageFilterOptionsAsync();
        await cacheService.RefreshPublicationTypeFilterOptionsAsync();

        var updated = await repository.GetByIdAsync(id);
        if (updated is not null)
            await indexingService.IndexPublicationAsync(updated);
    }

    public async Task DeleteAsync(int id)
    {
        _ = await repository.GetByIdAsync(id)
            ?? throw new Exceptions.NotFoundException($"Publication {id} was not found.");
        await repository.DeleteAsync(id);
        await cacheService.RefreshAuthorFilterOptionsAsync();
        await cacheService.RefreshKeywordFilterOptionsAsync();
        await cacheService.RefreshLanguageFilterOptionsAsync();
        await cacheService.RefreshPublicationTypeFilterOptionsAsync();

        await indexingService.RemovePublicationAsync(id);
    }

    // ── Mapping helpers ────────────────────────────────────────────────────

    private static PublicationSummaryDto ToSummary(Publication p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Authors = p.Authors.Select(a =>
            string.IsNullOrWhiteSpace(a.MiddleName)
                ? $"{a.FirstName} {a.LastName}".Trim()
                : $"{a.FirstName} {a.MiddleName} {a.LastName}".Trim()
        ).ToList(),
        Year = p.Year,
        Keywords = p.Keywords.Count > 0 ? string.Join(", ", p.Keywords.Select(k => k.Value)) : null,
        Languages = p.Languages.Count > 0 ? string.Join(", ", p.Languages.Select(l => l.Value)) : null,
        PublicationTypes = p.PublicationTypes.Count > 0 ? string.Join(", ", p.PublicationTypes.Select(pt => pt.Value)) : null,
        AbstractSnippet = p.Abstract is { Length: > 200 }
            ? p.Abstract[..200] + "\u2026"
            : p.Abstract,
        PdfFileName = p.PdfFileName
    };

    private static PublicationDetailDto ToDetail(Publication p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Abstract = p.Abstract,
        Body = p.Body,
        Keywords = p.Keywords.Count > 0 ? string.Join(", ", p.Keywords.Select(k => k.Value)) : null,
        Languages = p.Languages.Count > 0 ? string.Join(", ", p.Languages.Select(l => l.Value)) : null,
        PublicationTypes = p.PublicationTypes.Count > 0 ? string.Join(", ", p.PublicationTypes.Select(pt => pt.Value)) : null,
        Year = p.Year,
        DOI = p.DOI,
        PdfFileName = p.PdfFileName,
        CreatedAt = p.CreatedAt,
        LastModified = p.LastModified,
        Authors = p.Authors.Select(a => new AuthorDto
        {
            Id = a.Id,
            FirstName = a.FirstName,
            MiddleName = a.MiddleName,
            LastName = a.LastName,
            Email = a.Email
        }).ToList()
    };

    private static Publication FromDetail(PublicationDetailDto dto) => new()
    {
        Title = dto.Title,
        Abstract = dto.Abstract,
        Body = dto.Body,
        Keywords = string.IsNullOrWhiteSpace(dto.Keywords)
            ? []
            : dto.Keywords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => new Keyword { Value = v })
                .ToList(),
        Languages = string.IsNullOrWhiteSpace(dto.Languages)
            ? []
            : dto.Languages
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => new Language { Value = v })
                .ToList(),
        PublicationTypes = string.IsNullOrWhiteSpace(dto.PublicationTypes)
            ? []
            : dto.PublicationTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => new PublicationType { Value = v })
                .ToList(),
        Year = dto.Year,
        DOI = dto.DOI,
        PdfFileName = dto.PdfFileName,
        Authors = dto.Authors.Select(a => new Author
        {
            Id = a.Id,
            FirstName = a.FirstName,
            MiddleName = a.MiddleName,
            LastName = a.LastName,
            Email = a.Email
        }).ToList()
    };
}
