using MediatR;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

/// <summary>
/// One-time first-role selection for a freshly-registered Unassigned account (Task 5A).
/// Student is granted immediately; Company/Consultant enter the admin onboarding
/// queue, carrying the profile details captured during onboarding.
/// </summary>
[Auditable(AuditAction.RoleChanged, "User")]
public sealed record SelectRoleCommand(
    string Role,
    OnboardingDetails? Details = null) : IRequest<AuthTokensDto>;

/// <summary>
/// Profile details a Company or Consultant fills in during onboarding, so the
/// admin reviews a complete request rather than a bare role pick.
/// </summary>
public sealed record OnboardingDetails(
    string? OrganizationLegalName,
    string? OrganizationWebsite,
    string? Biography,
    decimal? SessionFeeUsd,
    string[]? ExpertiseTags);
