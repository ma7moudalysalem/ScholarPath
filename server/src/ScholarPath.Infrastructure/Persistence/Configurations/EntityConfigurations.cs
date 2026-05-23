using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

// ===================== Application-level field encryption =====================

/// <summary>
/// Wires the AES-256-GCM <see cref="EncryptedStringConverter"/> onto the columns
/// that hold genuinely-sensitive free-text PII (SRS security NFR — a second layer
/// on top of Azure SQL TDE). Invoked from <c>ApplicationDbContext.OnModelCreating</c>.
/// <para>
/// <b>An encrypted column cannot be queried by value</b> — no <c>Where</c>,
/// <c>OrderBy</c>, <c>Join</c>, index or unique constraint may reference it. The
/// columns below were each grep-confirmed across the whole codebase to appear
/// only in <c>Select</c> projections and direct assignments, never in a query
/// predicate:
/// </para>
/// <list type="bullet">
///   <item><description>
///   <see cref="UserProfile.Biography"/> — free-text personal bio. Read only via
///   <c>Select</c> (ProfileMapper, ProfileCompletenessCalculator, the resource
///   publish-rule checks) and written by UpdateProfile. No index references it.
///   </description></item>
///   <item><description>
///   <see cref="ApplicationTracker.PersonalNotes"/> — the applicant's private
///   notes. Read only via <c>Select</c> (GetApplicationDetail, the data-export
///   job) and written by StartApplication. No index references it.
///   </description></item>
/// </list>
/// <para>
/// Deliberately NOT encrypted: emails / names (login + uniqueness lookups),
/// slugs, Stripe identifiers (webhook lookups), enums, foreign keys, indexed
/// columns, and <c>UserProfile.DateOfBirth</c> (a typed <c>date</c> column, not
/// free text — a string converter would change its storage type).
/// </para>
/// </summary>
public static class FieldEncryptionModelBuilderExtensions
{
    public static void ApplyFieldEncryption(this ModelBuilder builder, IFieldEncryptionService encryption)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(encryption);

        var converter = new EncryptedStringConverter(encryption);

        builder.Entity<UserProfile>()
            .Property(p => p.Biography)
            .HasConversion(converter);

        builder.Entity<ApplicationTracker>()
            .Property(a => a.PersonalNotes)
            .HasConversion(converter);
    }
}

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
        // Biography is AES-256-GCM encrypted at rest (see FieldEncryptionModelBuilderExtensions).
        // The Base64 ciphertext envelope is markedly longer than the up-to-4000-char
        // plaintext, so the column is widened to nvarchar(max): SQL Server caps a
        // bounded nvarchar at 4000, hence MAX is the only width that fits the
        // ciphertext. The plaintext length cap is still enforced by the validator.
        b.Property(p => p.Biography);
        b.Property(p => p.FieldOfStudy).HasMaxLength(200);
        b.Property(p => p.CurrentInstitution).HasMaxLength(200);
        b.Property(p => p.Nationality).HasMaxLength(64);
        b.Property(p => p.LinkedInUrl).HasMaxLength(2048);
        b.Property(p => p.WebsiteUrl).HasMaxLength(2048);
        b.Property(p => p.PortfolioUrl).HasMaxLength(2048);
        b.Property(p => p.Timezone).HasMaxLength(64);
        b.Property(p => p.OrganizationLegalName).HasMaxLength(200);
        b.Property(p => p.OrganizationWebsite).HasMaxLength(300);
        b.Property(p => p.OrganizationEmail).HasMaxLength(256);
        b.Property(p => p.OrganizationCountry).HasMaxLength(80);
        b.Property(p => p.OrganizationRegistrationNumber).HasMaxLength(100);
        b.Property(p => p.OrganizationTaxNumber).HasMaxLength(100);
        b.Property(p => p.CompanyType).HasMaxLength(40);
        b.Property(p => p.CompanyDescription).HasMaxLength(1000);
        b.Property(p => p.ContactPersonFullName).HasMaxLength(100);
        b.Property(p => p.ContactPersonPosition).HasMaxLength(100);
        b.Property(p => p.ContactPhoneNumber).HasMaxLength(40);
        // Conditional applicability fields (FR-ONB-03 — Auth alignment AUTH-CODE-03).
        b.Property(p => p.TaxNotApplicableReason).HasMaxLength(500);
        b.Property(p => p.LegalRegistrationNotApplicableReason).HasMaxLength(500);
        // Onboarding rejection feedback (FR-ONB-07 — Auth alignment AUTH-CODE-06).
        // Re-surfaced on the wizard when a rejected applicant resubmits.
        b.Property(p => p.LastOnboardingRejectionReason).HasMaxLength(2000);
        b.Property(p => p.ProfessionalTitle).HasMaxLength(150);
        b.Property(p => p.HighestDegree).HasMaxLength(150);
        b.Property(p => p.FieldOfExpertise).HasMaxLength(200);
        b.Property(p => p.Gpa).HasPrecision(4, 2);
        b.Property(p => p.SessionFeeUsd).HasPrecision(10, 2);
        // PB-005R: Company rating snapshot. Precision 3,2 fits 0.00..5.00.
        b.Property(p => p.CompanyAverageRating).HasPrecision(3, 2);
        // Filtered index drives the admin low-rated-companies queue cheaply —
        // typical population is the whole users table, but only the small
        // flagged subset is selected.
        b.HasIndex(p => p.CompanyLowRatingFlaggedAt)
            .HasFilter("[CompanyLowRatingFlaggedAt] IS NOT NULL")
            .HasDatabaseName("IX_UserProfiles_CompanyLowRatingFlagged");
        b.Property(p => p.AcademicLevel).HasConversion<string>().HasMaxLength(32);
        b.Property(p => p.StripeConnectAccountId).HasMaxLength(256);
        b.Property(p => p.StripeConnectStatus).HasConversion<string>().HasMaxLength(24);
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
        // Free-text fields used when the tracker is NOT linked to a platform scholarship.
        b.Property(a => a.ExternalTitle).HasMaxLength(300);
        b.Property(a => a.ExternalProvider).HasMaxLength(200);
        b.Property(a => a.DecisionReason).HasMaxLength(2000);
        // PersonalNotes is AES-256-GCM encrypted at rest (see FieldEncryptionModelBuilderExtensions).
        // Widened to nvarchar(max): the Base64 ciphertext is longer than the
        // up-to-4000-char plaintext and SQL Server caps a bounded nvarchar at 4000.
        // The plaintext length cap is still enforced by the validator.
        b.Property(a => a.PersonalNotes);
        b.Property(a => a.RowVersion).IsRowVersion();

        b.Ignore(a => a.IsActive);

        // Single-active-application rule: unique filtered index (FR-057).
        // SQL Server filtered-index predicates do NOT accept `NOT IN` — only
        // the limited set (=, <>, <, <=, >, >=, IS [NOT] NULL) combined with
        // AND. So the original NOT IN predicate has to be expanded into
        // three chained <> conjunctions. Same meaning, accepted syntax.
        // The `IS NOT NULL` clause excludes purely-external trackers (no
        // ScholarshipId link) from the uniqueness rule — a student may track
        // any number of off-platform scholarships in parallel.
        b.HasIndex(a => new { a.StudentId, a.ScholarshipId })
            .IsUnique()
            // SQL Server filtered-index predicates don't support NOT IN — expand
            // into three chained <> conjunctions (same semantics, accepted syntax).
            .HasFilter(
                $"[ScholarshipId] IS NOT NULL " +
                $"AND [Status] <> '{nameof(ApplicationStatus.Withdrawn)}' " +
                $"AND [Status] <> '{nameof(ApplicationStatus.Rejected)}' " +
                $"AND [Status] <> '{nameof(ApplicationStatus.Accepted)}'")
            .HasDatabaseName("UX_Applications_Student_Scholarship_Active");

        b.HasIndex(a => a.Status);
        b.HasQueryFilter(a => !a.IsDeleted);

        b.HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
        // ScholarshipId is now optional (purely-external trackers carry no link).
        b.HasOne(a => a.Scholarship).WithMany(s => s.Applications).HasForeignKey(a => a.ScholarshipId)
            .IsRequired(false)
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

public sealed class CompanyReviewRequestConfiguration : IEntityTypeConfiguration<CompanyReviewRequest>
{
    public void Configure(EntityTypeBuilder<CompanyReviewRequest> b)
    {
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(r => r.Currency).HasMaxLength(8);
        b.Property(r => r.ReviewFeeUsdSnapshot).HasPrecision(10, 2);
        b.Property(r => r.CancelReason).HasMaxLength(500);
        b.Property(r => r.RejectReason).HasMaxLength(500);
        b.Property(r => r.RowVersion).IsRowVersion();

        b.HasIndex(r => new { r.StudentId, r.Status });
        b.HasIndex(r => new { r.CompanyId, r.Status });
        b.HasIndex(r => r.ScholarshipId);
        b.HasIndex(r => r.PaymentId);

        // A student can have at most one live (non-terminal) request per
        // scholarship. Cancelled / RejectedByCompany / Expired / Failed /
        // Completed / Closed are all terminal and excluded from the filter.
        b.HasIndex(r => new { r.StudentId, r.ScholarshipId })
            .IsUnique()
            .HasFilter(
                $"[Status] = '{nameof(CompanyReviewRequestStatus.Draft)}' " +
                $"OR [Status] = '{nameof(CompanyReviewRequestStatus.Submitted)}' " +
                $"OR [Status] = '{nameof(CompanyReviewRequestStatus.Pending)}' " +
                $"OR [Status] = '{nameof(CompanyReviewRequestStatus.UnderReview)}'")
            .HasDatabaseName("UX_CompanyReviewRequests_Student_Scholarship_Active");

        b.HasQueryFilter(r => !r.IsDeleted);

        // Three FKs to Users (Student + Company) and one each to Scholarship
        // and Payment — explicit Restrict to avoid SQL Server's
        // multiple-cascade-paths error (1785).
        b.HasOne(r => r.Student).WithMany().HasForeignKey(r => r.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Company).WithMany().HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Scholarship).WithMany().HasForeignKey(r => r.ScholarshipId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Payment).WithMany().HasForeignKey(r => r.PaymentId).OnDelete(DeleteBehavior.SetNull);
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
        b.Property(bk => bk.MeetingRoomId).HasMaxLength(64);
        b.Property(bk => bk.RecordingId).HasMaxLength(256);
        b.Property(bk => bk.CancellationReason).HasConversion<string>().HasMaxLength(500); ;
        b.Property(bk => bk.StripePaymentIntentId).HasMaxLength(256);
        b.Property(bk => bk.PriceUsd).HasPrecision(10, 2);
        // Free-text note from the student — capped so a paste-bomb can't grow
        // a single booking row to multi-MB. The validator on the command also
        // limits it; this is the SQL belt-and-braces.
        b.Property(bk => bk.StudentNotes).HasMaxLength(2000);
        b.Property(bk => bk.RowVersion).IsRowVersion();
        b.HasIndex(bk => new { bk.ConsultantId, bk.ScheduledStartAt });

        // Task 5A — slot race guard: a consultant cannot hold two live bookings for
        // the same start time. The DB rejects the second concurrent insert, which
        // ApplicationDbContext.SaveChangesAsync surfaces as a ConflictException (409).
        b.HasIndex(bk => new { bk.ConsultantId, bk.ScheduledStartAt })
            .IsUnique()
            .HasFilter("[Status] IN ('Requested', 'Confirmed')")
            .HasDatabaseName("UX_Bookings_Consultant_Slot_Active");

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
        b.HasIndex(p => p.PayoutId);
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

        // PB-014 AC#1: at most one active config (EffectiveTo IS NULL) per payment type
        b.HasIndex(p => p.PaymentType)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL")
            .HasDatabaseName("UX_ProfitShareConfig_ActivePerType");
    }
}

public sealed class FinancialConfigRuleConfiguration : IEntityTypeConfiguration<FinancialConfigRule>
{
    public void Configure(EntityTypeBuilder<FinancialConfigRule> b)
    {
        b.Property(r => r.PaymentType).HasConversion<string>().HasMaxLength(32);
        b.Property(r => r.FeeKind).HasConversion<string>().HasMaxLength(16);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(r => r.FeePercentage).HasPrecision(5, 4);
        b.Property(r => r.ProfitSharePercentage).HasPrecision(5, 4);
        b.Property(r => r.Notes).HasMaxLength(1000);
        b.Property(r => r.RowVersion).IsRowVersion();
        b.HasIndex(r => new { r.PaymentType, r.Status });

        // FR-170: at most one Active rule per payment type.
        b.HasIndex(r => r.PaymentType)
            .IsUnique()
            .HasFilter("[Status] = 'Active'")
            .HasDatabaseName("UX_FinancialConfigRule_ActivePerType");
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

public sealed class ForumBookmarkConfiguration : IEntityTypeConfiguration<ForumBookmark>
{
    public void Configure(EntityTypeBuilder<ForumBookmark> b)
    {
        b.HasIndex(x => new { x.ForumPostId, x.UserId }).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasOne(x => x.ForumPost)
            .WithMany(p => p.Bookmarks)
            .HasForeignKey(x => x.ForumPostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ForumTagConfiguration : IEntityTypeConfiguration<ForumTag>
{
    public void Configure(EntityTypeBuilder<ForumTag> b)
    {
        b.Property(t => t.Name).IsRequired().HasMaxLength(30);
        b.Property(t => t.Slug).IsRequired().HasMaxLength(30);
        b.HasIndex(t => t.Slug).IsUnique();
    }
}

public sealed class ForumPostTagConfiguration : IEntityTypeConfiguration<ForumPostTag>
{
    public void Configure(EntityTypeBuilder<ForumPostTag> b)
    {
        b.HasKey(x => new { x.ForumPostId, x.ForumTagId });
        b.HasOne(x => x.ForumPost)
            .WithMany(p => p.PostTags)
            .HasForeignKey(x => x.ForumPostId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ForumTag)
            .WithMany(t => t.PostTags)
            .HasForeignKey(x => x.ForumTagId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.ForumTagId);
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

public sealed class RecommendationClickEventConfiguration : IEntityTypeConfiguration<RecommendationClickEvent>
{
    public void Configure(EntityTypeBuilder<RecommendationClickEvent> b)
    {
        b.Property(e => e.Source).IsRequired().HasMaxLength(16);
        b.HasIndex(e => new { e.UserId, e.ClickedAt });
        b.HasIndex(e => new { e.ScholarshipId, e.ClickedAt });
        b.HasIndex(e => e.AiInteractionId);

        b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Scholarship).WithMany().HasForeignKey(e => e.ScholarshipId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.AiInteraction).WithMany().HasForeignKey(e => e.AiInteractionId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class AiRedactionAuditSampleConfiguration : IEntityTypeConfiguration<AiRedactionAuditSample>
{
    public void Configure(EntityTypeBuilder<AiRedactionAuditSample> b)
    {
        b.Property(s => s.RedactedPrompt).IsRequired().HasMaxLength(8000);
        b.Property(s => s.Verdict).HasConversion<string?>().HasMaxLength(24);
        b.HasIndex(s => s.SampledAt);
        b.HasIndex(s => s.AiInteractionId).IsUnique();          // one sample per interaction max
        b.HasIndex(s => new { s.Verdict, s.SampledAt });

        b.HasOne(s => s.AiInteraction).WithMany().HasForeignKey(s => s.AiInteractionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(s => s.Reviewer).WithMany().HasForeignKey(s => s.ReviewerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> b)
    {
        b.Property(d => d.SourceType).HasConversion<string>().HasMaxLength(24);
        b.Property(d => d.SourceKey).IsRequired().HasMaxLength(200);
        b.Property(d => d.TitleEn).IsRequired().HasMaxLength(300);
        b.Property(d => d.TitleAr).IsRequired().HasMaxLength(300);
        b.Property(d => d.ContentEn).IsRequired();
        b.Property(d => d.ContentAr).IsRequired();
        b.Property(d => d.ContentHash).IsRequired().HasMaxLength(64);
        b.Property(d => d.EmbeddingModel).HasMaxLength(64);
        b.Property(d => d.RowVersion).IsRowVersion();

        // Computed flag — not a column.
        b.Ignore(d => d.IsEmbedded);

        // One row per source object — the natural upsert / dedup key.
        b.HasIndex(d => new { d.SourceType, d.SourceKey }).IsUnique();
        b.HasIndex(d => d.SourceId);
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

public sealed class UserRiskFlagConfiguration : IEntityTypeConfiguration<UserRiskFlag>
{
    public void Configure(EntityTypeBuilder<UserRiskFlag> b)
    {
        // One row per user — reverse-ETL upserts on UserId.
        b.HasIndex(f => f.UserId).IsUnique();
        b.Property(f => f.Score).HasPrecision(5, 4);        // 0.0000 .. 1.0000
        b.Property(f => f.Reason).HasMaxLength(500);
        b.HasIndex(f => new { f.IsAtRisk, f.ComputedAt }); // admin "list at-risk" query path

        b.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
    public void Configure(EntityTypeBuilder<PlatformSetting> b)
    {
        b.Property(s => s.Key).IsRequired().HasMaxLength(200);
        b.Property(s => s.Value).IsRequired().HasMaxLength(4000);
        b.Property(s => s.ValueType).HasConversion<string>().HasMaxLength(16);
        b.Property(s => s.Category).IsRequired().HasMaxLength(100);
        b.Property(s => s.DescriptionEn).HasMaxLength(1000);
        b.Property(s => s.DescriptionAr).HasMaxLength(1000);
        b.Property(s => s.RowVersion).IsRowVersion();

        // PB-011: keys are the lookup identity — exactly one row per key.
        b.HasIndex(s => s.Key).IsUnique();
        b.HasIndex(s => s.Category);
    }
}
