using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class AuthorRepository(AppDbCntx context) : IAuthorRepository
{
    public async Task<(IEnumerable<Author> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = context.Authors
            .AsNoTracking()
            .Include(a => a.Publications)
            .OrderBy(a => a.FullName);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Author?> GetByIdAsync(int id) =>
        await context.Authors
            .AsNoTracking()
            .Include(a => a.Publications)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<int> CreateAsync(Author author)
    {
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        return author.Id;
    }

    public async Task UpdateAsync(Author author)
    {
        var existing = await context.Authors.FindAsync(author.Id)
            ?? throw new InvalidOperationException($"Author {author.Id} not found.");

        existing.FullName = author.FullName;
        existing.FirstName = author.FirstName;
        existing.LastName = author.LastName;
        existing.Email = author.Email;
        existing.LastModified = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var author = await context.Authors
            .Include(a => a.Publications)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (author is not null)
        {
            author.Publications.Clear();
            context.Authors.Remove(author);
            await context.SaveChangesAsync();
        }
    }
}
