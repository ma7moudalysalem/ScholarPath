using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

public class UpgradeRequestConfiguration : IEntityTypeConfiguration<UpgradeRequest>
{
    public void Configure(EntityTypeBuilder<UpgradeRequest> builder)
    {
        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.RequestedRole)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ur => ur.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ur => ur.AdminNotes)
            .HasMaxLength(1000);

        builder.Property(ur => ur.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(ur => ur.RejectionReasons)
            .HasMaxLength(2000);

        builder.Property(ur => ur.ReviewedBy)
            .HasMaxLength(200);

        // Consultant fields
        builder.Property(ur => ur.ExperienceSummary)
            .HasMaxLength(1500);

        builder.Property(ur => ur.ExpertiseTags)
            .HasMaxLength(500);

        builder.Property(ur => ur.Languages)
            .HasMaxLength(500);

        builder.Property(ur => ur.LinkedInUrl)
            .HasMaxLength(500);

        builder.Property(ur => ur.PortfolioUrl)
            .HasMaxLength(500);

        // Company fields
        builder.Property(ur => ur.CompanyName)
            .HasMaxLength(300);

        builder.Property(ur => ur.CompanyCountry)
            .HasMaxLength(100);

        builder.Property(ur => ur.CompanyWebsite)
            .HasMaxLength(500);

        builder.Property(ur => ur.ContactPersonName)
            .HasMaxLength(120);

        builder.Property(ur => ur.ContactEmail)
            .HasMaxLength(150);

        builder.Property(ur => ur.ContactPhone)
            .HasMaxLength(20);

        builder.Property(ur => ur.CompanyRegistrationNumber)
            .HasMaxLength(30);

        builder.Property(ur => ur.ProofDocumentUrl)
            .HasMaxLength(500);

        builder.HasIndex(ur => ur.Status)
            .HasDatabaseName("IX_UpgradeRequests_Status");

        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("IX_UpgradeRequests_UserId");

        builder.HasMany(ur => ur.EducationEntries)
            .WithOne(e => e.UpgradeRequest)
            .HasForeignKey(e => e.UpgradeRequestId);

        builder.HasMany(ur => ur.ExpertiseTagsList)
            .WithMany(t => t.UpgradeRequests)
            .UsingEntity("UpgradeRequestExpertiseTag");

        builder.HasMany(ur => ur.Links)
            .WithOne(l => l.UpgradeRequest)
            .HasForeignKey(l => l.UpgradeRequestId);

        builder.HasMany(ur => ur.Files)
            .WithOne(f => f.UpgradeRequest)
            .HasForeignKey(f => f.UpgradeRequestId);
    }
}

public class EducationEntryConfiguration : IEntityTypeConfiguration<EducationEntry>
{
    public void Configure(EntityTypeBuilder<EducationEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.InstitutionName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(e => e.DegreeName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.FieldOfStudy)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(e => e.UpgradeRequestId)
            .HasDatabaseName("IX_EducationEntries_UpgradeRequestId");
    }
}

public class ExpertiseTagConfiguration : IEntityTypeConfiguration<ExpertiseTag>
{
    public void Configure(EntityTypeBuilder<ExpertiseTag> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasDatabaseName("IX_ExpertiseTags_Name");
    }
}

public class UpgradeRequestLinkConfiguration : IEntityTypeConfiguration<UpgradeRequestLink>
{
    public void Configure(EntityTypeBuilder<UpgradeRequestLink> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.Label)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(l => l.UpgradeRequestId)
            .HasDatabaseName("IX_UpgradeRequestLinks_UpgradeRequestId");
    }
}

public class UpgradeRequestFileConfiguration : IEntityTypeConfiguration<UpgradeRequestFile>
{
    public void Configure(EntityTypeBuilder<UpgradeRequestFile> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(f => f.FilePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(f => f.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(f => f.UpgradeRequestId)
            .HasDatabaseName("IX_UpgradeRequestFiles_UpgradeRequestId");
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(n => n.TitleAr)
            .HasMaxLength(300);

        builder.Property(n => n.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(n => n.MessageAr)
            .HasMaxLength(2000);

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(100);

        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notifications_UserId");

        builder.HasIndex(n => n.IsRead)
            .HasDatabaseName("IX_Notifications_IsRead");
    }
}

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(r => r.TitleAr)
            .HasMaxLength(300);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.DescriptionAr)
            .HasMaxLength(1000);

        builder.Property(r => r.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Type)
            .HasMaxLength(50);

        builder.Property(r => r.Category)
            .HasMaxLength(100);
    }
}

public class SuccessStoryConfiguration : IEntityTypeConfiguration<SuccessStory>
{
    public void Configure(EntityTypeBuilder<SuccessStory> builder)
    {
        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(ss => ss.TitleAr)
            .HasMaxLength(300);

        builder.Property(ss => ss.Content)
            .IsRequired();

        builder.Property(ss => ss.ImageUrl)
            .HasMaxLength(500);

        builder.Property(ss => ss.ApprovedBy)
            .HasMaxLength(200);

        builder.HasIndex(ss => ss.IsApproved)
            .HasDatabaseName("IX_SuccessStories_IsApproved");

        builder.HasOne(ss => ss.User)
            .WithMany()
            .HasForeignKey(ss => ss.UserId);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(50);

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(50);

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(500);

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        // Ignore computed properties
        builder.Ignore(rt => rt.IsRevoked);
        builder.Ignore(rt => rt.IsExpired);
    }
}

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(up => up.Id);

        builder.Property(up => up.FieldOfStudy)
            .HasMaxLength(200);

        builder.Property(up => up.GPA)
            .HasPrecision(4, 2);

        builder.Property(up => up.Interests)
            .HasMaxLength(2000);

        builder.Property(up => up.Country)
            .HasMaxLength(100);

        builder.Property(up => up.TargetCountry)
            .HasMaxLength(100);

        builder.Property(up => up.Bio)
            .HasMaxLength(2000);

        builder.HasIndex(up => up.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserProfiles_UserId");
    }
}
