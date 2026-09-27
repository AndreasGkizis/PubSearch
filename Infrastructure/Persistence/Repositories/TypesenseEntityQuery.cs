using Microsoft.EntityFrameworkCore;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

internal static class TypesenseEntityQuery
{
    public static async Task<(IEnumerable<T> Items, int TotalCount)> PageAsync<T>(
        IQueryable<T> query, IEntitySearchIndex index, EntityIndexKind kind, string? search, int page, int pageSize)
        where T : BaseDbEntity
    {
        if (string.IsNullOrWhiteSpace(search))
            return (await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(), await query.CountAsync());

        var (ids, total) = await index.SearchAsync(kind, search, page, pageSize);
        var selectedIds = ids.ToArray();
        var items = await query.Where(item => selectedIds.Contains(item.Id)).ToListAsync();
        var byId = items.ToDictionary(item => item.Id);
        return (ids.Where(byId.ContainsKey).Select(id => byId[id]), total);
    }
}
