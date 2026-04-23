using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.ReviewApplication;

[Auditable(AuditAction.Update, "ApplicationTracker",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Company reviewed application {ApplicationId} with status {Status}")]
public sealed record ReviewApplicationCommand(
    Guid ApplicationId,
    ApplicationStatus Status,
    string? DecisionReason) : IRequest<bool>;
