using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;

[Auditable(AuditAction.Update, "Scholarship",
    TargetIdProperty = nameof(ScholarshipId),
    SummaryTemplate = "User configured review fee for scholarship {ScholarshipId}")]
public sealed record ConfigureReviewFeeCommand(
    Guid ScholarshipId,
    decimal ReviewFeeUsd) : IRequest<bool>;
