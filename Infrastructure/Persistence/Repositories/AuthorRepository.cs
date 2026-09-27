using ResearchPublications.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class AuthorRepository(AppDbCntx context, IEntitySearchIndex index) : IAuthorRepository
{
    public async Task<(IEnumerable<Author> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? search = null)
    {
        var query = context.Authors.AsNoTracking()
            .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
            .Select(a => new Author
            {
                Id = a.Id,
                FirstName = a.FirstName,
                MiddleName = a.MiddleName,
                LastName = a.LastName,
                Email = a.Email,
                CreatedAt = a.CreatedAt,
                LastModified = a.LastModified,
                PublicationCount = a.Publications.Count
            });
        return await TypesenseEntityQuery.PageAsync(query, index, EntityIndexKind.Authors, search, page, pageSize);
    }

    public async Task<Author?> GetByIdAsync(int id) =>
        await context.Authors
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new Author
            {
                Id = a.Id,
                FirstName = a.FirstName,
                MiddleName = a.MiddleName,
                LastName = a.LastName,
                Email = a.Email,
                CreatedAt = a.CreatedAt,
                LastModified = a.LastModified,
                PublicationCount = a.Publications.Count
            })
            .FirstOrDefaultAsync();

    public async Task<int> CreateAsync(Author author)
    {
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        await index.SynchronizeAsync(EntityIndexKind.Authors);
        return author.Id;
    }

    public async Task UpdateAsync(Author author)
    {
        var existing = await context.Authors.FindAsync(author.Id)
            ?? throw new InvalidOperationException($"Author {author.Id} not found.");

        existing.FirstName = author.FirstName;
        existing.MiddleName = author.MiddleName;
        existing.LastName = author.LastName;
        existing.Email = author.Email;
        existing.LastModified = DateTime.UtcNow;

        await context.SaveChangesAsync();
        await index.SynchronizeAsync(EntityIndexKind.Authors);
    }

    public async Task DeleteAsync(int id)
    {
        var author = await context.Authors
            .FirstOrDefaultAsync(a => a.Id == id);

        if (author is not null)
        {
            context.Authors.Remove(author);
            await context.SaveChangesAsync();
            await index.SynchronizeAsync(EntityIndexKind.Authors);
        }
    }

    public async Task<IEnumerable<(string Name, int Count)>> GetFilterOptionsAsync()
    {
        var results = await context.Authors
            .Select(a => new {
                Name = a.FirstName + (a.MiddleName != null ? " " + a.MiddleName : "") + " " + a.LastName,
                Count = a.Publications.Count
            })
            .OrderBy(x => x.Name)
            .ToListAsync();
        return results.Select(x => (x.Name, x.Count));
    }

    public async Task<IEnumerable<Author>> SearchAsync(string query, int limit)
    {
        var (items, _) = await GetAllAsync(1, limit, query);
        return items;
    }
}
