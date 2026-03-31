using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class PublicationTypeRepository(AppDbCntx context) : IPublicationTypeRepository
{
    public async Task<(IEnumerable<PublicationType> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = context.PublicationTypes
            .AsNoTracking()
            .OrderBy(pt => pt.Value);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pt => new PublicationType
            {
                Id = pt.Id,
                Value = pt.Value,
                CreatedAt = pt.CreatedAt,
                LastModified = pt.LastModified,
                PublicationCount = pt.Publications.Count
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<PublicationType?> GetByIdAsync(int id) =>
        await context.PublicationTypes
            .AsNoTracking()
            .Where(pt => pt.Id == id)
            .Select(pt => new PublicationType
            {
                Id = pt.Id,
                Value = pt.Value,
                CreatedAt = pt.CreatedAt,
                LastModified = pt.LastModified,
                PublicationCount = pt.Publications.Count
            })
            .FirstOrDefaultAsync();

    public async Task<PublicationType?> GetByValueAsync(string value) =>
        await context.PublicationTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(pt => pt.Value == value);

    public async Task<int> CreateAsync(PublicationType publicationType)
    {
        context.PublicationTypes.Add(publicationType);
        await context.SaveChangesAsync();
        return publicationType.Id;
    }

    public async Task UpdateAsync(PublicationType publicationType)
    {
        var existing = await context.PublicationTypes.FindAsync(publicationType.Id)
            ?? throw new InvalidOperationException($"PublicationType {publicationType.Id} not found.");

        existing.Value = publicationType.Value;
        existing.LastModified = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var publicationType = await context.PublicationTypes
            .FirstOrDefaultAsync(pt => pt.Id == id);

        if (publicationType is not null)
        {
            context.PublicationTypes.Remove(publicationType);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<(string Name, int Count)>> GetFilterOptionsAsync()
    {
        var results = await context.PublicationTypes
            .Select(pt => new { Name = pt.Value, Count = pt.Publications.Count })
            .OrderBy(x => x.Name)
            .ToListAsync();
        return results.Select(x => (x.Name, x.Count));
    }

    public async Task<IEnumerable<PublicationType>> SearchAsync(string query, int limit)
    {
        return await context.PublicationTypes
            .AsNoTracking()
            .Where(pt => pt.Value.ToLower().Contains(query.ToLower()))
            .OrderBy(pt => pt.Value)
            .Take(limit)
            .Select(pt => new PublicationType
            {
                Id = pt.Id,
                Value = pt.Value,
                PublicationCount = pt.Publications.Count
            })
            .ToListAsync();
    }
}
