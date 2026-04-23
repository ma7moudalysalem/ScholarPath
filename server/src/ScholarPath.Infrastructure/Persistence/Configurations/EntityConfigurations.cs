using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

// ============================== Identity ==============================

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        b.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        b.Property(u => u.ProfileImageUrl).HasMaxLength(2048);
        b.Property(u => u.AccountStatus).HasConversion<string>().HasMaxLength(32);
        b.Property(u => u.PreferredLanguage).HasMaxLength(8);
        b.Property(u => u.CountryOfResidence).HasMaxLength(64);
        b.Property(u => u.ActiveRole).HasMaxLength(32);
        b.Property(u => u.RowVersion).IsRowVersion();

        b.HasQueryFilter(u => !u.IsDeleted);
        b.HasIndex(u => u.AccountStatus);
        b.HasIndex(u => u.IsOnboardingComplete);

        b.HasOne(u => u.Profile).WithOne(p => p.User!).HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Ignore(u => u.DomainEvents);
    }
}

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.Property(p => p.Biography).HasMaxLength(4000);
        b.Property(p => p.FieldOfStudy).HasMaxLength(200);
        b.Property(p => p.CurrentInstitution).HasMaxLength(200);
        b.Property(p => p.Nationality).HasMaxLength(64);
        b.Property(p => p.LinkedInUrl).HasMaxLength(2048);
        b.Property(p => p.WebsiteUrl).HasMaxLength(2048);
        b.Property(p => p.Timezone).HasMaxLength(64);
        b.Property(p => p.Gpa).HasPrecision(4, 2);
        b.Property(p => p.SessionFeeUsd).HasPrecision(10, 2);
        b.Property(p => p.AcademicLevel).HasConversion<string>().HasMaxLength(32);
        b.Property(p => p.RowVersion).IsRowVersion();
        b.HasIndex(p => p.UserId).IsUnique();

        b.HasMany(p => p.EducationEntries).WithOne(e => e.UserProfile!)
            .HasForeignKey(e => e.UserProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.Property(t => t.TokenHash).IsRequired().HasMaxLength(512);
        b.HasIndex(t => t.TokenHash).IsUnique();
        b.HasIndex(t => new { t.UserId, t.IsRevoked });
        b.Property(t => t.IpAddress).HasMaxLength(64);
        b.Property(t => t.UserAgent).HasMaxLength(512);
        b.Property(t => t.RevokedReason).HasMaxLength(256);
        b.Property(t => t.RowVersion).IsRowVersion();

        b.HasOne(t => t.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> b)
    {
        b.Property(a => a.Email).IsRequired().HasMaxLength(256);
        b.Property(a => a.FailureReason).HasMaxLength(256);
        b.Property(a => a.IpAddress).HasMaxLength(64);
        b.Property(a => a.UserAgent).HasMaxLength(512);
        b.HasIndex(a => new { a.Email, a.OccurredAt });
    }
}

public sealed class EducationEntryConfiguration : IEntityTypeConfiguration<EducationEntry>
{
    public void Configure(EntityTypeBuilder<EducationEntry> b)
    {
        b.Property(e => e.InstitutionName).IsRequired().HasMaxLength(200);
        b.Property(e => e.Degree).IsRequired().HasMaxLength(200);
        b.Property(e => e.FieldOfStudy).IsRequired().HasMaxLength(200);
        b.Property(e => e.Description).HasMaxLength(2000);
        b.Property(e => e.Gpa).HasPrecision(4, 2);
        b.Property(e => e.RowVersion).IsRowVersion();
    }
}

public sealed class ExpertiseTagConfiguration : IEntityTypeConfiguration<ExpertiseTag>
{
    public void Configure(EntityTypeBuilder<ExpertiseTag> b)
    {
        b.Property(t => t.NameEn).IsRequired().HasMaxLength(100);
        b.Property(t => t.NameAr).IsRequired().HasMaxLength(100);
        b.Property(t => t.Slug).IsRequired().HasMaxLength(120);
        b.Property(t => t.Category).HasMaxLength(50);
        b.HasIndex(t => t.Slug).IsUnique();
    }
}

public sealed class UpgradeRequestConfiguration : IEntityTypeConfiguration<UpgradeRequest>
{
    public void Configure(EntityTypeBuilder<UpgradeRequest> b)
    {
        b.Property(r => r.Target).HasConversion<string>().HasMaxLength(16);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(r => r.Reason).HasMaxLength(2000);
        b.Property(r => r.ReviewerNotes).HasMaxLength(2000);
        b.Property(r => r.RowVersion).IsRowVersion();
        b.HasIndex(r => new { r.UserId, r.Status });
        b.HasQueryFilter(r => !r.IsDeleted);

        b.HasMany(r => r.Files).WithOne().HasForeignKey(f => f.UpgradeRequestId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(r => r.Links).WithOne().HasForeignKey(l => l.UpgradeRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UpgradeRequestFileConfiguration : IEntityTypeConfiguration<UpgradeRequestFile>
{
    public void Configure(EntityTypeBuilder<UpgradeRequestFile> b)
    {
        b.Property(f => f.FileName).IsRequired().HasMaxLength(512);
        b.Property(f => f.BlobUrl).IsRequired().HasMaxLength(2048);
        b.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
    }
}

public sealed class UpgradeRequestLinkConfiguration : IEntityTypeConfiguration<UpgradeRequestLink>
{
    public void Configure(EntityTypeBuilder<UpgradeRequestLink> b)
    {
        b.Property(l => l.Label).IsRequired().HasMaxLength(200);
        b.Property(l => l.Url).IsRequired().HasMaxLength(2048);
    }
}

// ============================== Scholarships ==============================

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(c => c.NameEn).IsRequired().HasMaxLength(100);
        b.Property(c => c.NameAr).IsRequired().HasMaxLength(100);
        b.Property(c => c.Slug).IsRequired().HasMaxLength(120);
        b.Property(c => c.DescriptionEn).HasMaxLength(1000);
        b.Property(c => c.DescriptionAr).HasMaxLength(1000);
        b.Property(c => c.IconKey).HasMaxLength(64);
        b.HasIndex(c => c.Slug).IsUnique();
        b.Property(c => c.RowVersion).IsRowVersion();
    }
}

public sealed class ScholarshipConfiguration : IEntityTypeConfiguration<Scholarship>
{
    public void Configure(EntityTypeBuilder<Scholarship> b)
    {
        b.Property(s => s.TitleEn).IsRequired().HasMaxLength(300);
        b.Property(s => s.TitleAr).IsRequired().HasMaxLength(300);
        b.Property(s => s.DescriptionEn).IsRequired().HasMaxLength(4000);
        b.Property(s => s.DescriptionAr).IsRequired().HasMaxLength(4000);
        b.Property(s => s.Slug).IsRequired().HasMaxLength(320);
        b.Property(s => s.Mode).HasConversion<string>().HasMaxLength(16);
        b.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(s => s.FundingType).HasConversion<string>().HasMaxLength(24);
        b.Property(s => s.TargetLevel).HasConversion<string>().HasMaxLength(24);
        b.Property(s => s.Currency).HasMaxLength(8);
        b.Property(s => s.ExternalApplicationUrl).HasMaxLength(2048);
        b.Property(s => s.EligibilityRequirementsEn).HasMaxLength(4000);
        b.Property(s => s.EligibilityRequirementsAr).HasMaxLength(4000);
        b.Property(s => s.FundingAmountUsd).HasPrecision(14, 2);
        b.Property(s => s.ReviewFeeUsd).HasPrecision(10, 2);
        b.Property(s => s.RowVersion).IsRowVersion();

        b.HasIndex(s => s.Slug).IsUnique();
        b.HasIndex(s => s.Status);
        b.HasIndex(s => s.Deadline);
        b.HasIndex(s => new { s.Status, s.Deadline });
        b.HasIndex(s => s.IsFeatured);
        b.HasIndex(s => s.Mode);

        b.HasQueryFilter(s => !s.IsDeleted);
        b.HasOne(s => s.Category).WithMany(c => c.Scholarships).HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasOne(s => s.OwnerCompany).WithMany().HasForeignKey(s => s.OwnerCompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ScholarshipChildConfiguration : IEntityTypeConfiguration<ScholarshipChild>
{
    public void Configure(EntityTypeBuilder<ScholarshipChild> b)
    {
        b.Property(c => c.ChildType).IsRequired().HasMaxLength(50);
        b.Property(c => c.KeyEn).IsRequired().HasMaxLength(300);
        b.Property(c => c.KeyAr).HasMaxLength(300);
        b.Property(c => c.ValueEn).HasMaxLength(2000);
        b.Property(c => c.ValueAr).HasMaxLength(2000);
        b.HasIndex(c => new { c.ScholarshipId, c.ChildType });
    }
}

public sealed class SavedScholarshipConfiguration : IEntityTypeConfiguration<SavedScholarship>
{
    public void Configure(EntityTypeBuilder<SavedScholarship> b)
    {
        b.HasIndex(s => new { s.UserId, s.ScholarshipId }).IsUnique();
        b.Property(s => s.Note).HasMaxLength(1000);
    }
}

// ============================== Applications ==============================

public sealed class ApplicationTrackerConfiguration : IEntityTypeConfiguration<ApplicationTracker>
{
    public void Configure(EntityTypeBuilder<ApplicationTracker> b)
    {
        b.Property(a => a.Mode).HasConversion<string>().HasMaxLength(16);
        b.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(a => a.ExternalTrackingUrl).HasMaxLength(2048);
        b.Property(a => a.ExternalReferenceId).HasMaxLength(256);
        b.Property(a => a.DecisionReason).HasMaxLength(2000);
        b.Property(a => a.PersonalNotes).HasMaxLength(4000);
        b.Property(a => a.RowVersion).IsRowVersion();

        b.Ignore(a => a.IsActive);

        // Single-active-application rule: unique filtered index (FR-057).
        // SQL Server filtered-index predicates do NOT accept `NOT IN` — only
        // the limited set (=, <>, <, <=, >, >=, IS [NOT] NULL) combined with
        // AND. So the original NOT IN predicate has to be expanded into
        // three chained <> conjunctions. Same meaning, accepted syntax.
        b.HasIndex(a => new { a.StudentId, a.ScholarshipId })
            .IsUnique()
            .HasFilter(
                $"[Status] <> '{nameof(ApplicationStatus.Withdrawn)}' " +
                $"AND [Status] <> '{nameof(ApplicationStatus.Rejected)}' " +
                $"AND [Status] <> '{nameof(ApplicationStatus.Accepted)}'");

        b.HasIndex(a => a.Status);
        b.HasQueryFilter(a => !a.IsDeleted);

        b.HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Scholarship).WithMany(s => s.Applications).HasForeignKey(a => a.ScholarshipId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ApplicationTrackerChildConfiguration : IEntityTypeConfiguration<ApplicationTrackerChild>
{
    public void Configure(EntityTypeBuilder<ApplicationTrackerChild> b)
    {
        b.Property(c => c.ChildType).IsRequired().HasMaxLength(50);
        b.Property(c => c.Title).HasMaxLength(300);
        b.Property(c => c.Content).HasMaxLength(4000);
        b.HasIndex(c => new { c.ApplicationTrackerId, c.ChildType });
    }
}

// ============================== Ratings + Bookings ==============================

public sealed class CompanyReviewConfiguration : IEntityTypeConfiguration<CompanyReview>
{
    public void Configure(EntityTypeBuilder<CompanyReview> b)
    {
        b.Property(r => r.Comment).HasMaxLength(2000);
        b.Property(r => r.AdminNote).HasMaxLength(1000);
        b.Property(r => r.RowVersion).IsRowVersion();
        b.HasIndex(r => new { r.CompanyId, r.IsHiddenByAdmin, r.IsDeleted });
        b.HasIndex(r => r.ApplicationTrackerId).IsUnique();
        b.HasQueryFilter(r => !r.IsDeleted);

        // Two FKs to Users (Student + Company) — explicit Restrict to avoid
        // SQL Server's multiple-cascade-paths error (1785).
        b.HasOne(r => r.Student).WithMany().HasForeignKey(r => r.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Company).WithMany().HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.ApplicationTracker).WithMany().HasForeignKey(r => r.ApplicationTrackerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompanyReviewPaymentConfiguration : IEntityTypeConfiguration<CompanyReviewPayment>
{
    public void Configure(EntityTypeBuilder<CompanyReviewPayment> b)
    {
        b.Property(p => p.StripePaymentIntentId).IsRequired().HasMaxLength(256);
        b.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(128);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(p => p.RefundReason).HasMaxLength(500);
        b.Property(p => p.AmountUsd).HasPrecision(14, 2);
        b.Property(p => p.ProfitShareAmountUsd).HasPrecision(14, 2);
        b.Property(p => p.PayeeAmountUsd).HasPrecision(14, 2);
        b.Property(p => p.RefundedAmountUsd).HasPrecision(14, 2);
        b.Property(p => p.RowVersion).IsRowVersion();
        b.HasIndex(p => p.StripePaymentIntentId).IsUnique();
        b.HasIndex(p => p.IdempotencyKey).IsUnique();
    }
}

public sealed class ConsultantReviewConfiguration : IEntityTypeConfiguration<ConsultantReview>
{
    public void Configure(EntityTypeBuilder<ConsultantReview> b)
    {
        b.Property(r => r.Comment).HasMaxLength(2000);
        b.Property(r => r.AdminNote).HasMaxLength(1000);
        b.Property(r => r.RowVersion).IsRowVersion();
        b.HasIndex(r => new { r.ConsultantId, r.IsHiddenByAdmin, r.IsDeleted });
        b.HasIndex(r => r.BookingId).IsUnique();
        b.HasQueryFilter(r => !r.IsDeleted);

        // Two FKs to Users (Student + Consultant) + one to Booking — explicit
        // Restrict to avoid SQL Server's multiple-cascade-paths error (1785).
        b.HasOne(r => r.Student).WithMany().HasForeignKey(r => r.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Consultant).WithMany().HasForeignKey(r => r.ConsultantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Booking).WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConsultantAvailabilityConfiguration : IEntityTypeConfiguration<ConsultantAvailability>
{
    public void Configure(EntityTypeBuilder<ConsultantAvailability> b)
    {
        b.Property(a => a.Timezone).IsRequired().HasMaxLength(64);
        b.Property(a => a.RowVersion).IsRowVersion();
        b.HasIndex(a => new { a.ConsultantId, a.IsActive });
        b.HasIndex(a => new { a.ConsultantId, a.DayOfWeek, a.StartTime, a.IsActive });
        b.HasIndex(a => new { a.ConsultantId, a.SpecificStartAt, a.IsActive });
        b.HasQueryFilter(a => !a.IsDeleted);
        b.HasOne(a => a.Consultant).WithMany().HasForeignKey(a => a.ConsultantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConsultantBookingConfiguration : IEntityTypeConfiguration<ConsultantBooking>
{
    public void Configure(EntityTypeBuilder<ConsultantBooking> b)
    {
        b.Property(bk => bk.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(bk => bk.MeetingUrl).HasMaxLength(2048);
        b.Property(bk => bk.CancellationReason).HasConversion<string>().HasMaxLength(500); ;
        b.Property(bk => bk.StripePaymentIntentId).HasMaxLength(256);
        b.Property(bk => bk.PriceUsd).HasPrecision(10, 2);
        b.Property(bk => bk.RowVersion).IsRowVersion();
        b.HasIndex(bk => new { bk.ConsultantId, bk.ScheduledStartAt });
        b.HasIndex(bk => new { bk.StudentId, bk.Status });
        b.HasIndex(bk => bk.StripePaymentIntentId);
        b.HasIndex(bk => bk.AvailabilityId);
        b.HasIndex(bk => bk.Status);
        b.HasQueryFilter(bk => !bk.IsDeleted);

        b.HasOne(bk => bk.Student).WithMany().HasForeignKey(bk => bk.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(bk => bk.Consultant).WithMany().HasForeignKey(bk => bk.ConsultantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(bk => bk.Availability).WithMany().HasForeignKey(bk => bk.AvailabilityId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(bk => bk.Payment).WithMany().HasForeignKey(bk => bk.PaymentId).OnDelete(DeleteBehavior.SetNull);
    }
}

// ============================== Payments ==============================

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(p => p.Type).HasConversion<string>().HasMaxLength(32);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(p => p.Currency).HasMaxLength(8);
        b.Property(p => p.StripePaymentIntentId).HasMaxLength(256);
        b.Property(p => p.StripeChargeId).HasMaxLength(256);
        b.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(128);
        b.Property(p => p.RefundReason).HasMaxLength(500);
        b.Property(p => p.FailureReason).HasMaxLength(500);
        b.Property(p => p.RowVersion).IsRowVersion();
        b.HasIndex(p => p.IdempotencyKey).IsUnique();
        b.HasIndex(p => p.StripePaymentIntentId);
        b.HasIndex(p => new { p.PayerUserId, p.Status });
        b.HasIndex(p => new { p.PayeeUserId, p.Status });
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}

public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> b)
    {
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(p => p.Currency).HasMaxLength(8);
        b.Property(p => p.StripePayoutId).HasMaxLength(256);
        b.Property(p => p.StripeConnectAccountId).HasMaxLength(256);
        b.Property(p => p.FailureReason).HasMaxLength(500);
        b.Property(p => p.RowVersion).IsRowVersion();
        b.HasIndex(p => new { p.PayeeUserId, p.Status });
    }
}

public sealed class StripeWebhookEventConfiguration : IEntityTypeConfiguration<StripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEvent> b)
    {
        b.Property(e => e.StripeEventId).IsRequired().HasMaxLength(256);
        b.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        b.Property(e => e.RawPayload).IsRequired();
        b.Property(e => e.ProcessingError).HasMaxLength(2000);
        b.HasIndex(e => e.StripeEventId).IsUnique();
        b.HasIndex(e => new { e.IsProcessed, e.ReceivedAt });
    }
}

public sealed class ProfitShareConfigConfiguration : IEntityTypeConfiguration<ProfitShareConfig>
{
    public void Configure(EntityTypeBuilder<ProfitShareConfig> b)
    {
        b.Property(p => p.PaymentType).HasConversion<string>().HasMaxLength(32);
        b.Property(p => p.Percentage).HasPrecision(5, 4);
        b.Property(p => p.Notes).HasMaxLength(1000);
        b.Property(p => p.RowVersion).IsRowVersion();
        b.HasIndex(p => new { p.PaymentType, p.EffectiveTo });
    }
}

// ============================== Community + Chat ==============================

public sealed class ForumCategoryConfiguration : IEntityTypeConfiguration<ForumCategory>
{
    public void Configure(EntityTypeBuilder<ForumCategory> b)
    {
        b.Property(c => c.NameEn).IsRequired().HasMaxLength(100);
        b.Property(c => c.NameAr).IsRequired().HasMaxLength(100);
        b.Property(c => c.Slug).IsRequired().HasMaxLength(120);
        b.Property(c => c.DescriptionEn).HasMaxLength(500);
        b.Property(c => c.DescriptionAr).HasMaxLength(500);
        b.HasIndex(c => c.Slug).IsUnique();
        b.Property(c => c.RowVersion).IsRowVersion();
    }
}

public sealed class ForumPostConfiguration : IEntityTypeConfiguration<ForumPost>
{
    public void Configure(EntityTypeBuilder<ForumPost> b)
    {
        b.Property(p => p.Title).HasMaxLength(500);
        b.Property(p => p.BodyMarkdown).IsRequired().HasMaxLength(10000);
        b.Property(p => p.ModerationStatus).HasConversion<string>().HasMaxLength(24);
        b.Property(p => p.ModerationNote).HasMaxLength(1000);
        b.Property(p => p.RowVersion).IsRowVersion();
        b.HasIndex(p => new { p.CategoryId, p.CreatedAt });
        b.HasIndex(p => p.ParentPostId);
        b.HasIndex(p => p.IsAutoHidden);
        b.HasQueryFilter(p => !p.IsDeleted);

        b.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Category).WithMany(c => c.Posts).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.ParentPost).WithMany(p => p.Replies).HasForeignKey(p => p.ParentPostId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ForumPostAttachmentConfiguration : IEntityTypeConfiguration<ForumPostAttachment>
{
    public void Configure(EntityTypeBuilder<ForumPostAttachment> b)
    {
        b.Property(a => a.FileName).IsRequired().HasMaxLength(512);
        b.Property(a => a.BlobUrl).IsRequired().HasMaxLength(2048);
        b.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
    }
}

public sealed class ForumVoteConfiguration : IEntityTypeConfiguration<ForumVote>
{
    public void Configure(EntityTypeBuilder<ForumVote> b)
    {
        b.Property(v => v.VoteType).HasConversion<string>().HasMaxLength(8);
        b.HasIndex(v => new { v.ForumPostId, v.UserId }).IsUnique();
    }
}

public sealed class ForumFlagConfiguration : IEntityTypeConfiguration<ForumFlag>
{
    public void Configure(EntityTypeBuilder<ForumFlag> b)
    {
        b.Property(f => f.Reason).IsRequired().HasMaxLength(200);
        b.Property(f => f.AdditionalDetails).HasMaxLength(1000);
        b.HasIndex(f => new { f.ForumPostId, f.FlaggedByUserId }).IsUnique();
    }
}

public sealed class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> b)
    {
        b.Property(c => c.RowVersion).IsRowVersion();
        b.HasIndex(c => new { c.ParticipantOneId, c.ParticipantTwoId }).IsUnique();
        b.HasIndex(c => c.LastMessageAt);
    }
}

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> b)
    {
        b.Property(m => m.Body).IsRequired().HasMaxLength(4000);
        b.Property(m => m.RowVersion).IsRowVersion();
        b.HasIndex(m => new { m.ConversationId, m.SentAt });
        b.HasQueryFilter(m => !m.IsDeleted);

        b.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> b)
    {
        b.Property(u => u.Reason).HasMaxLength(500);
        b.HasIndex(u => new { u.BlockerId, u.BlockedUserId }).IsUnique();
    }
}

// ============================== AI ==============================

public sealed class AiInteractionConfiguration : IEntityTypeConfiguration<AiInteraction>
{
    public void Configure(EntityTypeBuilder<AiInteraction> b)
    {
        b.Property(a => a.Feature).HasConversion<string>().HasMaxLength(24);
        b.Property(a => a.Provider).HasConversion<string>().HasMaxLength(16);
        b.Property(a => a.ModelName).HasMaxLength(100);
        b.Property(a => a.SessionId).HasMaxLength(128);
        b.Property(a => a.PromptText).IsRequired().HasMaxLength(8000);
        b.Property(a => a.ResponseText).IsRequired().HasMaxLength(16000);
        b.Property(a => a.CostUsd).HasPrecision(14, 6);
        b.Property(a => a.ErrorMessage).HasMaxLength(2000);
        b.Property(a => a.RowVersion).IsRowVersion();
        b.HasIndex(a => new { a.UserId, a.StartedAt });
        b.HasIndex(a => a.SessionId);
    }
}

// ============================== Resources ==============================

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> b)
    {
        b.Property(r => r.TitleEn).IsRequired().HasMaxLength(300);
        b.Property(r => r.TitleAr).IsRequired().HasMaxLength(300);
        b.Property(r => r.Slug).IsRequired().HasMaxLength(320);
        b.Property(r => r.DescriptionEn).HasMaxLength(2000);
        b.Property(r => r.DescriptionAr).HasMaxLength(2000);
        b.Property(r => r.ExternalLinkUrl).HasMaxLength(2048);
        b.Property(r => r.CoverImageUrl).HasMaxLength(2048);
        b.Property(r => r.AuthorRole).IsRequired().HasMaxLength(32);
        b.Property(r => r.Type).HasConversion<string>().HasMaxLength(24);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(r => r.CategorySlug).HasMaxLength(120);
        b.Property(r => r.RejectionReason).HasMaxLength(2000);
        b.Property(r => r.RowVersion).IsRowVersion();

        b.HasIndex(r => r.Slug).IsUnique();
        b.HasIndex(r => new { r.Status, r.IsFeatured });
        b.HasIndex(r => r.AuthorUserId);
        b.HasQueryFilter(r => !r.IsDeleted);
    }
}

public sealed class ResourceChildConfiguration : IEntityTypeConfiguration<ResourceChild>
{
    public void Configure(EntityTypeBuilder<ResourceChild> b)
    {
        b.Property(c => c.TitleEn).IsRequired().HasMaxLength(300);
        b.Property(c => c.TitleAr).IsRequired().HasMaxLength(300);
        b.HasIndex(c => new { c.ResourceId, c.SortOrder });
    }
}

public sealed class ResourceBookmarkConfiguration : IEntityTypeConfiguration<ResourceBookmark>
{
    public void Configure(EntityTypeBuilder<ResourceBookmark> b)
    {
        b.HasIndex(r => new { r.UserId, r.ResourceId }).IsUnique();
    }
}

public sealed class ResourceProgressConfiguration : IEntityTypeConfiguration<ResourceProgress>
{
    public void Configure(EntityTypeBuilder<ResourceProgress> b)
    {
        b.HasIndex(r => new { r.UserId, r.ResourceId }).IsUnique();
        b.Property(r => r.RowVersion).IsRowVersion();
    }
}

public sealed class ResourceProgressChildConfiguration : IEntityTypeConfiguration<ResourceProgressChild>
{
    public void Configure(EntityTypeBuilder<ResourceProgressChild> b)
    {
        b.HasIndex(c => new { c.ResourceProgressId, c.ResourceChildId }).IsUnique();
    }
}

// ============================== Notifications + cross-cutting ==============================

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.Property(n => n.Type).HasConversion<string>().HasMaxLength(48);
        b.Property(n => n.Channel).HasConversion<string>().HasMaxLength(16);
        b.Property(n => n.TitleEn).IsRequired().HasMaxLength(300);
        b.Property(n => n.TitleAr).IsRequired().HasMaxLength(300);
        b.Property(n => n.BodyEn).IsRequired().HasMaxLength(2000);
        b.Property(n => n.BodyAr).IsRequired().HasMaxLength(2000);
        b.Property(n => n.DeepLink).HasMaxLength(2048);
        b.Property(n => n.IdempotencyKey).HasMaxLength(128);
        b.Property(n => n.DispatchError).HasMaxLength(2000);
        b.Property(n => n.RowVersion).IsRowVersion();
        b.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt });
        b.HasIndex(n => n.IdempotencyKey);
        b.HasQueryFilter(n => !n.IsDeleted);
    }
}

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> b)
    {
        b.Property(p => p.Type).HasConversion<string>().HasMaxLength(48);
        b.Property(p => p.Channel).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(p => new { p.UserId, p.Type, p.Channel }).IsUnique();
        b.Property(p => p.RowVersion).IsRowVersion();
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(a => a.Action).HasConversion<string>().HasMaxLength(32);
        b.Property(a => a.TargetType).IsRequired().HasMaxLength(100);
        b.Property(a => a.IpAddress).HasMaxLength(64);
        b.Property(a => a.UserAgent).HasMaxLength(512);
        b.Property(a => a.CorrelationId).HasMaxLength(128);
        b.Property(a => a.Summary).HasMaxLength(2000);
        b.HasIndex(a => new { a.TargetType, a.TargetId });
        b.HasIndex(a => a.ActorUserId);
        b.HasIndex(a => a.OccurredAt);
    }
}

public sealed class UserDataRequestConfiguration : IEntityTypeConfiguration<UserDataRequest>
{
    public void Configure(EntityTypeBuilder<UserDataRequest> b)
    {
        b.Property(r => r.Type).HasConversion<string>().HasMaxLength(16);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(r => r.DownloadUrl).HasMaxLength(2048);
        b.Property(r => r.FailureReason).HasMaxLength(2000);
        b.Property(r => r.RowVersion).IsRowVersion();
        b.HasIndex(r => new { r.UserId, r.Type, r.Status });
    }
}

public sealed class SuccessStoryConfiguration : IEntityTypeConfiguration<SuccessStory>
{
    public void Configure(EntityTypeBuilder<SuccessStory> b)
    {
        b.Property(s => s.AuthorDisplayName).IsRequired().HasMaxLength(200);
        b.Property(s => s.AuthorImageUrl).HasMaxLength(2048);
        b.Property(s => s.HeadlineEn).IsRequired().HasMaxLength(300);
        b.Property(s => s.HeadlineAr).IsRequired().HasMaxLength(300);
        b.Property(s => s.BodyEn).IsRequired().HasMaxLength(4000);
        b.Property(s => s.BodyAr).IsRequired().HasMaxLength(4000);
        b.Property(s => s.ScholarshipNameEn).HasMaxLength(300);
        b.Property(s => s.ScholarshipNameAr).HasMaxLength(300);
        b.Property(s => s.CountryCode).HasMaxLength(8);
        b.Property(s => s.RowVersion).IsRowVersion();
        b.HasIndex(s => new { s.IsApproved, s.IsFeatured });
        b.HasQueryFilter(s => !s.IsDeleted);
    }
}
