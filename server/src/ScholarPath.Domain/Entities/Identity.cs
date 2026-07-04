using Microsoft.AspNetCore.Identity;
using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

/// <summary>
/// Extended Identity user. Inherits IdentityUser&lt;Guid&gt; so the Identity stack
/// can manage credentials while our business domain attributes live here.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, ISoftDeletable
{
    // Business fields
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? ProfileImageUrl { get; set; }
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Unassigned;
    public bool IsOnboardingComplete { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public string? PreferredLanguage { get; set; } = "en";
    public string? CountryOfResidence { get; set; }

    // Dual-role support: active role for the session
    public string? ActiveRole { get; set; }

    // Audit-friendly timestamps
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    // Navigation (lightweight — heavy collections live on dedicated entities)
    public UserProfile? Profile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; } = [];
    public ICollection<LoginAttempt> LoginAttempts { get; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Domain events plumbing (since we don't inherit BaseEntity)
    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void RaiseDomainEvent(DomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public ApplicationUser? User { get; set; }
}

public class LoginAttempt : BaseEntity
{
    public string Email { get; set; } = default!;
    public Guid? UserId { get; set; }
    public bool Succeeded { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class UserProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public string? Biography { get; set; }
    public string? BiographyAr { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Timezone { get; set; }

    // Student fields
    public AcademicLevel? AcademicLevel { get; set; }
    public string? FieldOfStudy { get; set; }
    public string? CurrentInstitution { get; set; }
    public decimal? Gpa { get; set; }
    public string? GpaScale { get; set; }
    public string? PreferredCountriesJson { get; set; }
    public string? PreferredFieldsJson { get; set; }

    // Student consultant-booking access (PB-006R, FR-CBR-21..24). A student who
    // late-cancels, no-shows (admin-validated), or falsely reports a consultant
    // no-show is temporarily blocked from creating NEW bookings. BookingBlockUntil
    // is the expiry; a lazy check flips the status back to Active once it passes.
    public BookingAccessStatus BookingAccessStatus { get; set; } = BookingAccessStatus.Active;
    public BookingBlockReason? BookingBlockReason { get; set; }
    public DateTimeOffset? BookingBlockUntil { get; set; }

    // ScholarshipProvider fields
    public string? OrganizationLegalName { get; set; }
    public string? OrganizationRegistrationNumber { get; set; }
    public string? OrganizationWebsite { get; set; }
    public string? OrganizationVerificationStatus { get; set; }
    public DateTimeOffset? OrganizationVerifiedAt { get; set; }
    // Extended company onboarding fields (FR-ONB-03) — collected during onboarding
    // so the admin reviewer has enough information to verify the organization.
    public string? OrganizationEmail { get; set; }
    public string? OrganizationCountry { get; set; }
    public string? OrganizationTaxNumber { get; set; }
    public string? ScholarshipProviderType { get; set; }
    public string? ScholarshipProviderDescription { get; set; }
    public string? ContactPersonFullName { get; set; }
    public string? ContactPersonPosition { get; set; }
    public string? ContactPhoneNumber { get; set; }

    // ── ScholarshipProvider rating snapshot (PB-005R low-rating policy) ───────────────────
    // Recalculated from ScholarshipProviderReviews on every new submission. Snapshotted on
    // UserProfile so dashboards + the admin low-rated queue don't re-aggregate
    // the whole reviews table on every page load.
    //
    //   ScholarshipProviderAverageRating     null when the company has no visible reviews
    //                            yet; otherwise the live average over reviews
    //                            that are neither soft-deleted nor admin-hidden.
    //   ScholarshipProviderReviewCount       count of the same visible reviews.
    //   ScholarshipProviderLowRatingFlaggedAt set when the average dipped below 2.5 after a
    //                            submission. Sticky: only an admin clears it
    //                            (via ClearScholarshipProviderLowRatingFlag or by
    //                            suspending the account). Subsequent sub-2.5
    //                            submissions do not overwrite the original
    //                            flagged-at timestamp.
    public decimal? ScholarshipProviderAverageRating { get; set; }
    public int ScholarshipProviderReviewCount { get; set; }
    public DateTimeOffset? ScholarshipProviderLowRatingFlaggedAt { get; set; }

    // Conditional applicability flags (FR-ONB-03 — SRS): a ScholarshipProvider that is not
    // tax-registered or not legally registered (e.g. a not-yet-incorporated
    // initiative) must supply a reason, in which case the corresponding numbers
    // / documents become optional.
    public bool? IsTaxRegistered { get; set; }
    public string? TaxNotApplicableReason { get; set; }
    public bool? IsLegallyRegistered { get; set; }
    public string? LegalRegistrationNotApplicableReason { get; set; }

    /// <summary>
    /// Latest admin rejection note from the onboarding queue, surfaced to the
    /// applicant so they see why their previous submission was rejected before
    /// resubmitting (FR-ONB-07).
    /// </summary>
    public string? LastOnboardingRejectionReason { get; set; }
    public DateTimeOffset? LastOnboardingRejectedAt { get; set; }

    // Consultant fields
    public decimal? SessionFeeUsd { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public string? ExpertiseTagsJson { get; set; }
    public string? LanguagesJson { get; set; }
    public DateTimeOffset? ConsultantVerifiedAt { get; set; }
    // Extended consultant onboarding fields (FR-ONB-04)
    public string? ProfessionalTitle { get; set; }
    public string? HighestDegree { get; set; }
    public string? FieldOfExpertise { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? PortfolioUrl { get; set; }

    /// <summary>
    /// When set, the consultant's booking intake is suspended pending admin
    /// review (FR-094 low-rating policy). The account itself stays active —
    /// only new booking requests are blocked. Cleared by an admin reinstatement.
    /// </summary>
    public DateTimeOffset? BookingIntakeSuspendedAt { get; set; }

    // Consultant rating snapshot + penalty ledger (PB-006R, FR-CBR-33..39).
    // Mirrors the ScholarshipProvider snapshot triplet above, but adds a
    // persisted penalty FACTOR so reputation deductions (cancel −20% / validated
    // no-show −40% / false report −70%) survive future review recomputes:
    //
    //   ConsultantAverageRating   the DISPLAYED average = round(clamp(rawAvg * factor, 0, 5), 2);
    //                             null when the consultant has no visible reviews.
    //   ConsultantReviewCount     count of visible (not soft-deleted / admin-hidden) reviews.
    //   ConsultantLowRatingFlaggedAt sticky admin-queue flag when the *penalized* average
    //                             dips below threshold. Distinct from BookingIntakeSuspendedAt
    //                             (which the *raw* trailing-window average drives).
    //   ConsultantRatingPenaltyFactor compounding multiplier, default 1.0; only a penalty
    //                             event mutates it (*= 0.80/0.60/0.30). A recompute never resets it.
    public decimal? ConsultantAverageRating { get; set; }
    public int ConsultantReviewCount { get; set; }
    public DateTimeOffset? ConsultantLowRatingFlaggedAt { get; set; }
    public decimal ConsultantRatingPenaltyFactor { get; set; } = 1.0m;

    // Stripe Connect — payee payout onboarding
    public string? StripeConnectAccountId { get; set; }
    public StripeConnectStatus StripeConnectStatus { get; set; } = StripeConnectStatus.None;
    public DateTimeOffset? StripeConnectOnboardedAt { get; set; }

    // Computed
    public int? ProfileCompletenessPercent { get; set; }

    public ApplicationUser? User { get; set; }
    public ICollection<EducationEntry> EducationEntries { get; } = [];
}

public class EducationEntry : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public string InstitutionName { get; set; } = default!;
    public string Degree { get; set; } = default!;
    public string FieldOfStudy { get; set; } = default!;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? Gpa { get; set; }
    public string? Description { get; set; }

    public UserProfile? UserProfile { get; set; }
}

public class ExpertiseTag : BaseEntity
{
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Category { get; set; }
}
