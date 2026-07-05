using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Commands.ReviewUpgradeRequest;

public sealed class ReviewUpgradeRequestCommandHandler(
    IApplicationDbContext db,
    IUserAdministration admin,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    INotificationDispatcher notifications,
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
                UpgradeTarget.ScholarshipProvider => "ScholarshipProvider",
                UpgradeTarget.Consultant => "Consultant",
                _ => throw new ConflictException($"Unknown upgrade target {req.Target}."),
            };

            await admin.AddRoleAsync(req.UserId, targetRole, ct).ConfigureAwait(false);
            await admin.SetAccountStatusAsync(req.UserId, AccountStatus.Active,
                $"Upgrade to {targetRole} approved.", ct).ConfigureAwait(false);

            // Granting the Consultant role is not enough — stamp the official
            // verification marker so the user passes IConsultantEligibilityService
            // (role switch, availability, marketplace). This keeps the approval a
            // single, atomic act of "you are now a consultant".
            if (req.Target == UpgradeTarget.Consultant)
            {
                await MarkConsultantVerifiedAsync(req.UserId, now, ct).ConfigureAwait(false);
            }

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

        // Tell the requester their upgrade was decided (in-app + email).
        await notifications.DispatchAsync(
            req.UserId,
            request.Decision == UpgradeDecision.Approve
                ? NotificationType.UpgradeRequestApproved
                : NotificationType.UpgradeRequestRejected,
            new NotificationParams
            {
                StatusText = req.Target.ToString(),
                Reason = request.ReviewerNotes,
            },
            deepLink: null,
            idempotencyKey: $"upgrade-review:{req.Id:N}",
            ct).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Sets <see cref="Domain.Entities.UserProfile.ConsultantVerifiedAt"/> — the
    /// official consultant approval marker — creating the profile row if the
    /// user somehow has none. Idempotent: an already-verified consultant keeps
    /// their original verification timestamp.
    /// </summary>
    private async Task MarkConsultantVerifiedAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        if (profile is null)
        {
            profile = new UserProfile { UserId = userId, CreatedAt = now };
            db.UserProfiles.Add(profile);
        }

        profile.ConsultantVerifiedAt ??= now;
    }
}
