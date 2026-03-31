using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class KeywordRepository(AppDbCntx context) : IKeywordRepository
{
    public async Task<(IEnumerable<Keyword> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = context.Keywords
            .AsNoTracking()
            .OrderBy(k => k.Value);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(k => new Keyword
            {
                Id = k.Id,
                Value = k.Value,
                CreatedAt = k.CreatedAt,
                LastModified = k.LastModified,
                PublicationCount = k.Publications.Count
            })
            .ToListAsync();

        return (items, total);
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
        return keyword.Id;
    }

    public async Task UpdateAsync(Keyword keyword)
    {
        var existing = await context.Keywords.FindAsync(keyword.Id)
            ?? throw new InvalidOperationException($"Keyword {keyword.Id} not found.");

        existing.Value = keyword.Value;
        existing.LastModified = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var keyword = await context.Keywords
            .FirstOrDefaultAsync(k => k.Id == id);

        if (keyword is not null)
        {
            context.Keywords.Remove(keyword);
            await context.SaveChangesAsync();
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
        return await context.Keywords
            .AsNoTracking()
            .Where(k => k.Value.ToLower().Contains(query.ToLower()))
            .OrderBy(k => k.Value)
            .Take(limit)
            .Select(k => new Keyword
            {
                Id = k.Id,
                Value = k.Value,
                PublicationCount = k.Publications.Count
            })
            .ToListAsync();
    }
}
