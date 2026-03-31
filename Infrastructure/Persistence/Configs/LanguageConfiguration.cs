using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Infrastructure.Persistence.Configs;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Value)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(l => l.Value)
            .IsUnique();

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.LastModified)
            .IsRequired();
    }
}
