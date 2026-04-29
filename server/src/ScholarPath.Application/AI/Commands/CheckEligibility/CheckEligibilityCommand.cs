using MediatR;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Commands.CheckEligibility;

[Auditable(AuditAction.Create, "AiInteraction",
    SummaryTemplate = "AI eligibility check for scholarship {ScholarshipId}")]
public sealed record CheckEligibilityCommand(Guid ScholarshipId) : IRequest<EligibilityDto>;
