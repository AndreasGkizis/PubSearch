using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class LanguageRepository(AppDbCntx context) : ILanguageRepository
{
    public async Task<(IEnumerable<Language> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = context.Languages
            .AsNoTracking()
            .OrderBy(l => l.Value);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new Language
            {
                Id = l.Id,
                Value = l.Value,
                CreatedAt = l.CreatedAt,
                LastModified = l.LastModified,
                PublicationCount = l.Publications.Count
            })
            .ToListAsync();

        return (items, total);
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
        return language.Id;
    }

    public async Task UpdateAsync(Language language)
    {
        var existing = await context.Languages.FindAsync(language.Id)
            ?? throw new InvalidOperationException($"Language {language.Id} not found.");

        existing.Value = language.Value;
        existing.LastModified = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var language = await context.Languages
            .FirstOrDefaultAsync(l => l.Id == id);

        if (language is not null)
        {
            context.Languages.Remove(language);
            await context.SaveChangesAsync();
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
        return await context.Languages
            .AsNoTracking()
            .Where(l => l.Value.ToLower().Contains(query.ToLower()))
            .OrderBy(l => l.Value)
            .Take(limit)
            .Select(l => new Language
            {
                Id = l.Id,
                Value = l.Value,
                PublicationCount = l.Publications.Count
            })
            .ToListAsync();
    }
}
