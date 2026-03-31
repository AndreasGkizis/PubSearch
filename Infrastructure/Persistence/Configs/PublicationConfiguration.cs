using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Infrastructure.Persistence.Configs;

public class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("Publications");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Abstract)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Body)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.DOI)
            .HasMaxLength(200);

        builder.Property(p => p.PdfFileName)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.LastModified)
            .IsRequired();

        builder.HasMany(p => p.Authors)
            .WithMany(a => a.Publications)
            .UsingEntity(j => j.ToTable("PublicationAuthors"));

        builder.HasMany(p => p.Keywords)
            .WithMany(k => k.Publications)
            .UsingEntity(j => j.ToTable("PublicationKeywords"));

        builder.HasMany(p => p.Languages)
            .WithMany(l => l.Publications)
            .UsingEntity(j => j.ToTable("PublicationLanguages"));

        builder.HasMany(p => p.PublicationTypes)
            .WithMany(pt => pt.Publications)
            .UsingEntity(j => j.ToTable("PublicationPublicationTypes"));
    }
}
