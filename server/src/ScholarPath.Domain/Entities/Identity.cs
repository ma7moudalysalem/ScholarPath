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

    // Company fields
    public string? OrganizationLegalName { get; set; }
    public string? OrganizationRegistrationNumber { get; set; }
    public string? OrganizationWebsite { get; set; }
    public string? OrganizationVerificationStatus { get; set; }
    public DateTimeOffset? OrganizationVerifiedAt { get; set; }

    // Consultant fields
    public decimal? SessionFeeUsd { get; set; }
    public int? SessionDurationMinutes { get; set; }
    public string? ExpertiseTagsJson { get; set; }
    public string? LanguagesJson { get; set; }
    public DateTimeOffset? ConsultantVerifiedAt { get; set; }

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
