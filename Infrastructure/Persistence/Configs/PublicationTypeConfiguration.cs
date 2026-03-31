using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Infrastructure.Persistence.Configs;

public class PublicationTypeConfiguration : IEntityTypeConfiguration<PublicationType>
{
    public void Configure(EntityTypeBuilder<PublicationType> builder)
    {
        builder.ToTable("PublicationTypes");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Value)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(pt => pt.Value)
            .IsUnique();

        builder.Property(pt => pt.CreatedAt)
            .IsRequired();

        builder.Property(pt => pt.LastModified)
            .IsRequired();
    }
}
