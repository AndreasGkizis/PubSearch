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
            .Include(k => k.Publications)
            .OrderBy(k => k.Value);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Keyword?> GetByIdAsync(int id) =>
        await context.Keywords
            .AsNoTracking()
            .Include(k => k.Publications)
            .FirstOrDefaultAsync(k => k.Id == id);

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
            .Include(k => k.Publications)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (keyword is not null)
        {
            keyword.Publications.Clear();
            context.Keywords.Remove(keyword);
            await context.SaveChangesAsync();
        }
    }
}
