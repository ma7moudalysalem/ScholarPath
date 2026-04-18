using MediatR;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Audit.Commands.RequestDataDelete;

[Auditable(AuditAction.Create, "UserDataRequest",
    SummaryTemplate = "Account deletion requested, processes after cooling period (id={TargetId})")]
public sealed record RequestDataDeleteCommand(string? Reason) : IRequest<DataRequestDto>;
