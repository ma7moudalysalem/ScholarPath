using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Audit.Commands.CancelDataDelete;

[Auditable(AuditAction.Update, "UserDataRequest",
    SummaryTemplate = "Account deletion cancelled (id={TargetId})")]
public sealed record CancelDataDeleteCommand : IRequest<bool>;
