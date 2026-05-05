using MediatR;

namespace ScholarPath.Application.UpgradeRequests.Commands.ApproveUpgradeRequest;

public record ApproveUpgradeRequestCommand(Guid Id, string? ReviewNotes) : IRequest<ApproveUpgradeResponse>;

public record ApproveUpgradeResponse(
    Guid Id,
    string Status,
    DateTime? ReviewedAt,
    Guid? ReviewedById);
