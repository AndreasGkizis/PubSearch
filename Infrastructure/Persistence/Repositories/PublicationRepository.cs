using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class PublicationRepository(AppDbCntx context) : IPublicationRepository
{
    public async Task<Publication?> GetByIdAsync(int id) =>
        await context.Publications
            .AsNoTracking()
            .Include(p => p.Authors)
            .Include(p => p.Keywords)
            .Include(p => p.Languages)
            .Include(p => p.PublicationTypes)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(IEnumerable<Publication> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize,
        int? yearFrom = null, int? yearTo = null,
        IReadOnlyList<string>? authors = null,
        IReadOnlyList<string>? keywords = null,
        IReadOnlyList<string>? languages = null,
        IReadOnlyList<string>? publicationTypes = null)
    {
        var query = context.Publications
            .AsNoTracking()
            .AsQueryable();

        if (yearFrom.HasValue)
            query = query.Where(p => p.Year >= yearFrom);

        if (yearTo.HasValue)
            query = query.Where(p => p.Year <= yearTo);

        if (authors is { Count: > 0 })
            query = query.Where(p => p.Authors.Any(a =>
                authors.Contains(a.FirstName + (a.MiddleName != null ? " " + a.MiddleName : "") + " " + a.LastName)));

        if (keywords is { Count: > 0 })
            query = query.Where(p => p.Keywords.Any(k => keywords.Contains(k.Value)));

        if (languages is { Count: > 0 })
            query = query.Where(p => p.Languages.Any(l => languages.Contains(l.Value)));

        if (publicationTypes is { Count: > 0 })
            query = query.Where(p => p.PublicationTypes.Any(pt => publicationTypes.Contains(pt.Value)));

        var total = await query.CountAsync();

        var projected = await query
            .OrderByDescending(p => p.LastModified)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Year,
                p.Abstract,
                p.PdfFileName,
                p.LastModified,
                p.CreatedAt,
                Authors = p.Authors.Select(a => new { a.Id, a.FirstName, a.MiddleName, a.LastName, a.Email }).ToList(),
                Keywords = p.Keywords.Select(k => new { k.Id, k.Value }).ToList(),
                Languages = p.Languages.Select(l => new { l.Id, l.Value }).ToList(),
                PublicationTypes = p.PublicationTypes.Select(pt => new { pt.Id, pt.Value }).ToList()
            })
            .ToListAsync();

        var items = projected.Select(p => new Publication
        {
            Id = p.Id,
            Title = p.Title,
            Year = p.Year,
            Abstract = p.Abstract,
            PdfFileName = p.PdfFileName,
            LastModified = p.LastModified,
            CreatedAt = p.CreatedAt,
            Authors = p.Authors.Select(a => new Author { Id = a.Id, FirstName = a.FirstName, MiddleName = a.MiddleName, LastName = a.LastName, Email = a.Email }).ToList(),
            Keywords = p.Keywords.Select(k => new Keyword { Id = k.Id, Value = k.Value }).ToList(),
            Languages = p.Languages.Select(l => new Language { Id = l.Id, Value = l.Value }).ToList(),
            PublicationTypes = p.PublicationTypes.Select(pt => new PublicationType { Id = pt.Id, Value = pt.Value }).ToList()
        });

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

        // Resolve languages: reuse existing records matched by value
        for (int i = 0; i < publication.Languages.Count; i++)
        {
            var lang = publication.Languages[i];
            var existing = await context.Languages.FirstOrDefaultAsync(l => l.Value == lang.Value);
            if (existing != null)
                publication.Languages[i] = existing;
        }

        // Resolve publication types: reuse existing records matched by value
        for (int i = 0; i < publication.PublicationTypes.Count; i++)
        {
            var pt = publication.PublicationTypes[i];
            var existing = await context.PublicationTypes.FirstOrDefaultAsync(t => t.Value == pt.Value);
            if (existing != null)
                publication.PublicationTypes[i] = existing;
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

            var byName = await context.Authors.FirstOrDefaultAsync(a =>
                a.FirstName == author.FirstName && a.MiddleName == author.MiddleName && a.LastName == author.LastName);
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
            .Include(p => p.Languages)
            .Include(p => p.PublicationTypes)
            .FirstOrDefaultAsync(p => p.Id == publication.Id)
            ?? throw new InvalidOperationException($"Publication {publication.Id} not found.");

        existing.Title = publication.Title;
        existing.Abstract = publication.Abstract;
        existing.Body = publication.Body;
        existing.Year = publication.Year;
        existing.DOI = publication.DOI;
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
            var byName = await context.Authors.FirstOrDefaultAsync(a =>
                a.FirstName == author.FirstName && a.MiddleName == author.MiddleName && a.LastName == author.LastName);
            existing.Authors.Add(byName ?? author);
        }

        existing.Keywords.Clear();
        foreach (var kw in publication.Keywords)
        {
            var resolved = await context.Keywords.FirstOrDefaultAsync(k => k.Value == kw.Value) ?? kw;
            existing.Keywords.Add(resolved);
        }

        existing.Languages.Clear();
        foreach (var lang in publication.Languages)
        {
            var resolved = await context.Languages.FirstOrDefaultAsync(l => l.Value == lang.Value) ?? lang;
            existing.Languages.Add(resolved);
        }

        existing.PublicationTypes.Clear();
        foreach (var pt in publication.PublicationTypes)
        {
            var resolved = await context.PublicationTypes.FirstOrDefaultAsync(t => t.Value == pt.Value) ?? pt;
            existing.PublicationTypes.Add(resolved);
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

}
