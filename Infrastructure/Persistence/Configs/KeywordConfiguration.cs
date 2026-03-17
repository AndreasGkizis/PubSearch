using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Infrastructure.Persistence.Configs;

public class KeywordConfiguration : IEntityTypeConfiguration<Keyword>
{
    public void Configure(EntityTypeBuilder<Keyword> builder)
    {
        builder.ToTable("Keywords");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Value)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(k => k.Value)
            .IsUnique();

        builder.Property(k => k.CreatedAt)
            .IsRequired();

        builder.Property(k => k.LastModified)
            .IsRequired();
    }
}
