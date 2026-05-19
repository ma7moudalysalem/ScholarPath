using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

/// <summary>
/// Consultant-session recording (PB-006). The bytes live in blob storage;
/// this row holds metadata and the storage key.
/// </summary>
public sealed class SessionRecordingConfiguration : IEntityTypeConfiguration<SessionRecording>
{
    public void Configure(EntityTypeBuilder<SessionRecording> b)
    {
        b.Property(r => r.RecordingId).IsRequired().HasMaxLength(256);
        b.Property(r => r.StoragePath).IsRequired().HasMaxLength(1024);
        b.Property(r => r.ContentType).IsRequired().HasMaxLength(150);
        b.Property(r => r.RowVersion).IsRowVersion();

        b.HasQueryFilter(r => !r.IsDeleted);
        b.HasIndex(r => r.BookingId);
        b.HasIndex(r => r.RecordingId);

        b.HasOne(r => r.Booking)
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
