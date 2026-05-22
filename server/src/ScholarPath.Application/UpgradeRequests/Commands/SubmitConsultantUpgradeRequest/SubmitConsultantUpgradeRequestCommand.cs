using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgradeRequest;

/// <summary>
/// Student-initiated request to be upgraded to Consultant (FR-ONB-07).
/// The submitted consultant profile fields are persisted on the user's
/// profile so the existing Admin review queue surfaces a complete request;
/// the upgrade itself is granted by the existing ReviewUpgradeRequest flow.
/// </summary>
[Auditable(AuditAction.RoleChanged, "UpgradeRequest")]
public sealed record SubmitConsultantUpgradeRequestCommand(
    string Biography,
    string ProfessionalTitle,
    string HighestDegree,
    string FieldOfExpertise,
    int? YearsOfExperience,
    decimal? SessionFeeUsd,
    int? SessionDurationMinutes,
    string[]? ExpertiseTags,
    string[]? Languages,
    string Country,
    string Timezone,
    string? LinkedInUrl = null,
    string? PortfolioUrl = null) : IRequest<Guid>;
