using ResearchPublications.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class KeywordRepository(AppDbCntx context, IEntitySearchIndex index) : IKeywordRepository
{
    public async Task<(IEnumerable<Keyword> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search = null)
    {
        var query = context.Keywords.AsNoTracking()
            .OrderBy(k => k.Value)
            .Select(k => new Keyword
            {
                Id = k.Id,
                Value = k.Value,
                CreatedAt = k.CreatedAt,
                LastModified = k.LastModified,
                PublicationCount = k.Publications.Count
            });
        return await TypesenseEntityQuery.PageAsync(query, index, EntityIndexKind.Keywords, search, page, pageSize);
    }

    public async Task<Keyword?> GetByIdAsync(int id) =>
        await context.Keywords
            .AsNoTracking()
            .Where(k => k.Id == id)
            .Select(k => new Keyword
            {
                Id = k.Id,
                Value = k.Value,
                CreatedAt = k.CreatedAt,
                LastModified = k.LastModified,
                PublicationCount = k.Publications.Count
            })
            .FirstOrDefaultAsync();

    public async Task<Keyword?> GetByValueAsync(string value) =>
        await context.Keywords
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Value == value);

    public async Task<int> CreateAsync(Keyword keyword)
    {
        context.Keywords.Add(keyword);
        await context.SaveChangesAsync();
        await index.SynchronizeAsync(EntityIndexKind.Keywords);
        return keyword.Id;
    }

    public async Task UpdateAsync(Keyword keyword)
    {
        var existing = await context.Keywords.FindAsync(keyword.Id)
            ?? throw new InvalidOperationException($"Keyword {keyword.Id} not found.");

        existing.Value = keyword.Value;
        existing.LastModified = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await index.SynchronizeAsync(EntityIndexKind.Keywords);
    }

    public async Task DeleteAsync(int id)
    {
        var keyword = await context.Keywords
            .FirstOrDefaultAsync(k => k.Id == id);

        if (keyword is not null)
        {
            context.Keywords.Remove(keyword);
            await context.SaveChangesAsync();
            await index.SynchronizeAsync(EntityIndexKind.Keywords);
        }
    }

    public async Task<IEnumerable<(string Name, int Count)>> GetFilterOptionsAsync()
    {
        var results = await context.Keywords
            .Select(k => new { Name = k.Value, Count = k.Publications.Count })
            .OrderBy(x => x.Name)
            .ToListAsync();
        return results.Select(x => (x.Name, x.Count));
    }

    public async Task<IEnumerable<Keyword>> SearchAsync(string query, int limit)
    {
        var (items, _) = await GetAllAsync(1, limit, query);
        return items;
    }
}
