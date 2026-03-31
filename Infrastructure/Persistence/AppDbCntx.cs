using Microsoft.EntityFrameworkCore;
using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Infrastructure.Persistence;

public class AppDbCntx(DbContextOptions<AppDbCntx> options) : DbContext(options)
{
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Keyword> Keywords => Set<Keyword>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<PublicationType> PublicationTypes => Set<PublicationType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbCntx).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
