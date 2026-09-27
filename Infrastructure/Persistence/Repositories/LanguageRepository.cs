using ResearchPublications.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class LanguageRepository(AppDbCntx context, IEntitySearchIndex index) : ILanguageRepository
{
    public async Task<(IEnumerable<Language> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search = null)
    {
        var query = context.Languages.AsNoTracking()
            .OrderBy(l => l.Value)
            .Select(l => new Language
            {
                Id = l.Id,
                Value = l.Value,
                CreatedAt = l.CreatedAt,
                LastModified = l.LastModified,
                PublicationCount = l.Publications.Count
            });
        return await TypesenseEntityQuery.PageAsync(query, index, EntityIndexKind.Languages, search, page, pageSize);
    }

    public async Task<Language?> GetByIdAsync(int id) =>
        await context.Languages
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new Language
            {
                Id = l.Id,
                Value = l.Value,
                CreatedAt = l.CreatedAt,
                LastModified = l.LastModified,
                PublicationCount = l.Publications.Count
            })
            .FirstOrDefaultAsync();

    public async Task<Language?> GetByValueAsync(string value) =>
        await context.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Value == value);

    public async Task<int> CreateAsync(Language language)
    {
        context.Languages.Add(language);
        await context.SaveChangesAsync();
        await index.SynchronizeAsync(EntityIndexKind.Languages);
        return language.Id;
    }

    public async Task UpdateAsync(Language language)
    {
        var existing = await context.Languages.FindAsync(language.Id)
            ?? throw new InvalidOperationException($"Language {language.Id} not found.");

        existing.Value = language.Value;
        existing.LastModified = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await index.SynchronizeAsync(EntityIndexKind.Languages);
    }

    public async Task DeleteAsync(int id)
    {
        var language = await context.Languages
            .FirstOrDefaultAsync(l => l.Id == id);

        if (language is not null)
        {
            context.Languages.Remove(language);
            await context.SaveChangesAsync();
            await index.SynchronizeAsync(EntityIndexKind.Languages);
        }
    }

    public async Task<IEnumerable<(string Name, int Count)>> GetFilterOptionsAsync()
    {
        var results = await context.Languages
            .Select(l => new { Name = l.Value, Count = l.Publications.Count })
            .OrderBy(x => x.Name)
            .ToListAsync();
        return results.Select(x => (x.Name, x.Count));
    }

    public async Task<IEnumerable<Language>> SearchAsync(string query, int limit)
    {
        var (items, _) = await GetAllAsync(1, limit, query);
        return items;
    }
}
