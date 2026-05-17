using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

/// <summary>Document-vault entity (FR-216). Bytes live in storage; this row holds metadata only.</summary>
public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.Property(d => d.FileName).IsRequired().HasMaxLength(260);
        b.Property(d => d.ContentType).IsRequired().HasMaxLength(150);
        b.Property(d => d.StoragePath).IsRequired().HasMaxLength(1024);
        b.Property(d => d.Category).HasConversion<string>().HasMaxLength(32);
        b.Property(d => d.RowVersion).IsRowVersion();

        b.HasQueryFilter(d => !d.IsDeleted);
        b.HasIndex(d => new { d.OwnerUserId, d.Category });
        b.HasIndex(d => d.ApplicationTrackerId);

        b.HasOne(d => d.Owner)
            .WithMany()
            .HasForeignKey(d => d.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(d => d.Application)
            .WithMany()
            .HasForeignKey(d => d.ApplicationTrackerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
