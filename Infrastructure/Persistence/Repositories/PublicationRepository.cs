using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class PublicationRepository(AppDbCntx context) : IPublicationRepository
{
    public async Task<Publication?> GetByIdAsync(int id) =>
        await context.Publications
            .Include(p => p.Authors)
            .Include(p => p.Keywords)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(IEnumerable<Publication> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize,
        int? yearFrom = null, int? yearTo = null,
        IReadOnlyList<string>? authors = null,
        IReadOnlyList<string>? keywords = null)
    {
        var query = context.Publications
            .Include(p => p.Authors)
            .Include(p => p.Keywords)
            .AsQueryable();

        if (yearFrom.HasValue)
            query = query.Where(p => p.Year >= yearFrom);

        if (yearTo.HasValue)
            query = query.Where(p => p.Year <= yearTo);

        if (authors is { Count: > 0 })
            query = query.Where(p => p.Authors.Any(a => authors.Contains(a.FullName)));

        if (keywords is { Count: > 0 })
            query = query.Where(p => p.Keywords.Any(k => keywords.Contains(k.Value)));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.LastModified)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<int> CreateAsync(Publication publication)
    {
        // Resolve keywords: reuse existing records matched by value
        for (int i = 0; i < publication.Keywords.Count; i++)
        {
            var kw = publication.Keywords[i];
            var existing = await context.Keywords.FirstOrDefaultAsync(k => k.Value == kw.Value);
            if (existing != null)
                publication.Keywords[i] = existing;
        }

        // Resolve authors: reuse existing records matched by ID or name
        for (int i = 0; i < publication.Authors.Count; i++)
        {
            var author = publication.Authors[i];
            if (author.Id > 0)
            {
                var tracked = await context.Authors.FindAsync(author.Id);
                if (tracked != null)
                {
                    publication.Authors[i] = tracked;
                    continue;
                }
            }

            var byName = await context.Authors.FirstOrDefaultAsync(a => a.FullName == author.FullName);
            if (byName != null)
                publication.Authors[i] = byName;
        }

        context.Publications.Add(publication);
        await context.SaveChangesAsync();
        return publication.Id;
    }

    public async Task UpdateAsync(Publication publication)
    {
        var existing = await context.Publications
            .Include(p => p.Authors)
            .Include(p => p.Keywords)
            .FirstOrDefaultAsync(p => p.Id == publication.Id)
            ?? throw new InvalidOperationException($"Publication {publication.Id} not found.");

        existing.Title = publication.Title;
        existing.Abstract = publication.Abstract;
        existing.Body = publication.Body;
        existing.Year = publication.Year;
        existing.DOI = publication.DOI;
        existing.CitationCount = publication.CitationCount;
        existing.PdfFileName = publication.PdfFileName;
        existing.LastModified = DateTime.UtcNow;

        existing.Authors.Clear();
        foreach (var author in publication.Authors)
        {
            if (author.Id > 0)
            {
                var tracked = await context.Authors.FindAsync(author.Id);
                if (tracked != null)
                {
                    existing.Authors.Add(tracked);
                    continue;
                }
            }

            // Resolve by name to avoid inserting duplicates when ID is unknown
            var byName = await context.Authors.FirstOrDefaultAsync(a => a.FullName == author.FullName);
            existing.Authors.Add(byName ?? author);
        }

        existing.Keywords.Clear();
        foreach (var kw in publication.Keywords)
        {
            var resolved = await context.Keywords.FirstOrDefaultAsync(k => k.Value == kw.Value) ?? kw;
            existing.Keywords.Add(resolved);
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var publication = await context.Publications.FindAsync(id);
        if (publication is not null)
        {
            context.Publications.Remove(publication);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<string>> GetAllAuthorsAsync() =>
        await context.Authors.Select(a => a.FullName).ToListAsync();

    public async Task<IEnumerable<string>> GetAllKeywordsAsync() =>
        await context.Keywords.Select(k => k.Value).ToListAsync();
}
