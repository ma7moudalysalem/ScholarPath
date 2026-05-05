using MediatR;

namespace ScholarPath.Application.UpgradeRequests.Commands.RejectUpgradeRequest;

public record RejectUpgradeRequestCommand(Guid Id, string? ReviewNotes, List<string>? RejectionReasons) : IRequest<RejectUpgradeResponse>;

public record RejectUpgradeResponse(
    Guid Id,
    string Status,
    string? AdminNotes,
    string? RejectionReasons,
    DateTime? ReviewedAt,
    Guid? ReviewedById);
