using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

public class ApplicationTrackerConfiguration : IEntityTypeConfiguration<ApplicationTracker>
{
    public void Configure(EntityTypeBuilder<ApplicationTracker> builder)
    {
        builder.HasKey(at => at.Id);

        builder.Property(at => at.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ApplicationStatus.Planned);

        builder.Property(at => at.Notes)
            .HasMaxLength(2000);

        builder.HasMany(at => at.ChecklistItems)
            .WithOne(c => c.ApplicationTracker)
            .HasForeignKey(c => c.ApplicationTrackerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(at => at.Reminders)
            .WithOne(r => r.ApplicationTracker)
            .HasForeignKey(r => r.ApplicationTrackerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on (UserId, ScholarshipId) excluding soft-deleted records
        builder.HasIndex(at => new { at.UserId, at.ScholarshipId })
            .IsUnique()
            .HasFilter("IsDeleted = 0")
            .HasDatabaseName("IX_ApplicationTrackers_UserId_ScholarshipId");

        // Relationships
        builder.HasOne(at => at.User)
            .WithMany()
            .HasForeignKey(at => at.UserId);

        builder.HasOne(at => at.Scholarship)
            .WithMany()
            .HasForeignKey(at => at.ScholarshipId);
    }
}
