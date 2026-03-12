using MediatR;

namespace ScholarPath.Application.UpgradeRequests.Commands.RequestMoreInfoUpgradeRequest;

public record RequestMoreInfoUpgradeRequestCommand(Guid Id, string? ReviewNotes) : IRequest<RequestMoreInfoUpgradeResponse>;

public record RequestMoreInfoUpgradeResponse(
    Guid Id,
    string Status,
    string? AdminNotes,
    DateTime? ReviewedAt,
    Guid? ReviewedById);
