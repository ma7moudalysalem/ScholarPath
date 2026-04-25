using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.WithdrawApplication;

[Auditable(AuditAction.Update, "ApplicationTracker",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Student withdrew application {ApplicationId}")]
public sealed record WithdrawApplicationCommand(
    Guid ApplicationId) : IRequest<bool>;
