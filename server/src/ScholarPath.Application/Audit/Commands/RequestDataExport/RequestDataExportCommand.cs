using MediatR;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Audit.Commands.RequestDataExport;

[Auditable(AuditAction.Create, "UserDataRequest",
    SummaryTemplate = "Data export requested (id={TargetId})")]
public sealed record RequestDataExportCommand : IRequest<DataRequestDto>;
