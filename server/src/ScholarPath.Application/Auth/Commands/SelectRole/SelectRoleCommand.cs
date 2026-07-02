using MediatR;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

/// <summary>
/// One-time first-role selection for a freshly-registered Unassigned account (Task 5A).
/// Student is granted immediately; ScholarshipProvider/Consultant enter the admin onboarding
/// queue, carrying the profile details captured during onboarding.
/// </summary>
[Auditable(AuditAction.RoleChanged, "User")]
public sealed record SelectRoleCommand(
    string Role,
    OnboardingDetails? Details = null) : IRequest<AuthTokensDto>;

/// <summary>
/// Profile details a ScholarshipProvider or Consultant fills in during onboarding, so the
/// admin reviews a complete request rather than a bare role pick. Companies
/// and Consultants populate disjoint subsets of these fields.
/// </summary>
public sealed record OnboardingDetails(
    // ── ScholarshipProvider ─────────────────────────────────────────────────────────────
    string? OrganizationLegalName = null,
    string? OrganizationWebsite = null,
    string? OrganizationEmail = null,
    string? OrganizationCountry = null,
    string? ScholarshipProviderType = null,
    string? ScholarshipProviderDescription = null,
    string? OrganizationRegistrationNumber = null,
    string? OrganizationTaxNumber = null,
    string? ContactPersonFullName = null,
    string? ContactPersonPosition = null,
    string? ContactPhoneNumber = null,
    // Conditional applicability flags (FR-ONB-03 / Auth alignment AUTH-CODE-03):
    // A ScholarshipProvider that is not tax-registered or not legally registered (e.g. a
    // not-yet-incorporated initiative) must supply a reason; the corresponding
    // numbers / documents become optional in that case.
    bool? IsTaxRegistered = null,
    string? TaxNotApplicableReason = null,
    bool? IsLegallyRegistered = null,
    string? LegalRegistrationNotApplicableReason = null,
    // ── Consultant ──────────────────────────────────────────────────────────
    string? Biography = null,
    string? ProfessionalTitle = null,
    string? HighestDegree = null,
    string? FieldOfExpertise = null,
    int? YearsOfExperience = null,
    decimal? SessionFeeUsd = null,
    int? SessionDurationMinutes = null,
    string[]? ExpertiseTags = null,
    string[]? Languages = null,
    string? Country = null,
    string? Timezone = null,
    string? LinkedInUrl = null,
    string? PortfolioUrl = null);
