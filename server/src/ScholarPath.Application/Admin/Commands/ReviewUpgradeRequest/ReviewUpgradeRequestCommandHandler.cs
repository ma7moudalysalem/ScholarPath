using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Commands.ReviewUpgradeRequest;

public sealed class ReviewUpgradeRequestCommandHandler(
    IApplicationDbContext db,
    IUserAdministration admin,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    ILogger<ReviewUpgradeRequestCommandHandler> logger)
    : IRequestHandler<ReviewUpgradeRequestCommand, bool>
{
    public async Task<bool> Handle(ReviewUpgradeRequestCommand request, CancellationToken ct)
    {
        var req = await db.UpgradeRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("UpgradeRequest", request.RequestId);

        if (req.Status != UpgradeRequestStatus.Pending)
        {
            throw new ConflictException(
                $"Upgrade request {req.Id} is already {req.Status}; only Pending requests can be reviewed.");
        }

        var now = clock.UtcNow;
        req.ReviewedAt = now;
        req.ReviewedByAdminId = currentUser.UserId;
        req.ReviewerNotes = request.ReviewerNotes;
        req.UpdatedAt = now;

        if (request.Decision == UpgradeDecision.Approve)
        {
            req.Status = UpgradeRequestStatus.Approved;

            var targetRole = req.Target switch
            {
                UpgradeTarget.Company => "Company",
                UpgradeTarget.Consultant => "Consultant",
                _ => throw new ConflictException($"Unknown upgrade target {req.Target}."),
            };

            await admin.AddRoleAsync(req.UserId, targetRole, ct).ConfigureAwait(false);
            await admin.SetAccountStatusAsync(req.UserId, AccountStatus.Active,
                $"Upgrade to {targetRole} approved.", ct).ConfigureAwait(false);

            logger.LogInformation("Approved upgrade {RequestId}: user {UserId} → {Role}",
                req.Id, req.UserId, targetRole);
        }
        else
        {
            req.Status = UpgradeRequestStatus.Rejected;
            logger.LogInformation("Rejected upgrade {RequestId} for user {UserId}. Notes: {Notes}",
                req.Id, req.UserId, request.ReviewerNotes);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
