using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.UpdateExternalStatus;
[Auditable(AuditAction.Update, "Application",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Updated external status for application {ApplicationId}")]
public record UpdateExternalStatusCommand(Guid Id, ApplicationStatus Status) : IRequest<bool>;
