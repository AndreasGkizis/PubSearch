using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Infrastructure.Persistence.Configs;

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FirstName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.MiddleName)
            .HasMaxLength(200);

        builder.Property(a => a.LastName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Email)
            .HasMaxLength(300);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.LastModified)
            .IsRequired();
    }
}
