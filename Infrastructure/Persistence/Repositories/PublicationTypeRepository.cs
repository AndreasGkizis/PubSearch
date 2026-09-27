using ResearchPublications.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class PublicationTypeRepository(AppDbCntx context, IEntitySearchIndex index) : IPublicationTypeRepository
{
    public async Task<(IEnumerable<PublicationType> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search = null)
    {
        var query = context.PublicationTypes.AsNoTracking()
            .OrderBy(pt => pt.Value)
            .Select(pt => new PublicationType
            {
                Id = pt.Id,
                Value = pt.Value,
                CreatedAt = pt.CreatedAt,
                LastModified = pt.LastModified,
                PublicationCount = pt.Publications.Count
            });
        return await TypesenseEntityQuery.PageAsync(query, index, EntityIndexKind.PublicationTypes, search, page, pageSize);
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
        await index.SynchronizeAsync(EntityIndexKind.PublicationTypes);
        return publicationType.Id;
    }

    public async Task UpdateAsync(PublicationType publicationType)
    {
        var existing = await context.PublicationTypes.FindAsync(publicationType.Id)
            ?? throw new InvalidOperationException($"PublicationType {publicationType.Id} not found.");

        existing.Value = publicationType.Value;
        existing.LastModified = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await index.SynchronizeAsync(EntityIndexKind.PublicationTypes);
    }

    public async Task DeleteAsync(int id)
    {
        var publicationType = await context.PublicationTypes
            .FirstOrDefaultAsync(pt => pt.Id == id);

        if (publicationType is not null)
        {
            context.PublicationTypes.Remove(publicationType);
            await context.SaveChangesAsync();
            await index.SynchronizeAsync(EntityIndexKind.PublicationTypes);
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
        var (items, _) = await GetAllAsync(1, limit, query);
        return items;
    }
}
