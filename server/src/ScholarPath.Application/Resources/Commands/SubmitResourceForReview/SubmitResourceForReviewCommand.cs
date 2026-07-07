using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Commands.SubmitResourceForReview;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Submits a draft resource for admin review (PB-009 AC#3). A consultant/company
/// draft moves to <c>PendingReview</c>; an admin's own draft is published directly.
/// </summary>
[Auditable(AuditAction.Update, "Resource",
    TargetIdProperty = nameof(ResourceId),
    SummaryTemplate = "Submitted resource {ResourceId} for review")]
public sealed record SubmitResourceForReviewCommand(Guid ResourceId) : IRequest<ResourceStatus>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class SubmitResourceForReviewCommandValidator
    : AbstractValidator<SubmitResourceForReviewCommand>
{
    public SubmitResourceForReviewCommandValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class SubmitResourceForReviewCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<SubmitResourceForReviewCommandHandler> logger)
    : IRequestHandler<SubmitResourceForReviewCommand, ResourceStatus>
{
    public async Task<ResourceStatus> Handle(
        SubmitResourceForReviewCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var resource = await db.Resources
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId, ct)
            ?? throw new NotFoundException(nameof(Resource), request.ResourceId);

        var isAdmin = currentUser.IsAdminOrSuperAdmin();
        if (resource.AuthorUserId != userId && !isAdmin)
            throw new ForbiddenAccessException("You can only submit your own resources.");

        if (resource.Status != ResourceStatus.Draft)
            throw new ConflictException("Only a draft resource can be submitted for review.");

        // PB-009 AC#8 — the resource must be complete before it leaves Draft.
        var authorBio = await db.UserProfiles
            .Where(p => p.UserId == resource.AuthorUserId)
            .Select(p => p.Biography)
            .FirstOrDefaultAsync(ct);

        var blockers = ResourcePublishRules.FindBlockers(resource, authorBio);
        if (blockers.Count > 0)
            throw new ConflictException(
                "Resource is not ready: " + string.Join(" ", blockers));

        var now = DateTimeOffset.UtcNow;

        // Admins publish directly; everyone else enters the review queue (AC#3).
        if (isAdmin)
        {
            resource.Status = ResourceStatus.Published;
            resource.PublishedAt = now;
            resource.ReviewedAt = now;
            resource.ReviewedByAdminId = userId;
            resource.RejectionReason = null;
        }
        else
        {
            resource.Status = ResourceStatus.PendingReview;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // PB-009 AC#3 — a resource that entered the review queue must surface in
        // the admin moderation inbox. An admin's own draft published directly
        // (no queue), so only notify on the PendingReview path. Best-effort:
        // a notification failure must never break the submission itself.
        if (resource.Status == ResourceStatus.PendingReview)
        {
            await NotifyAdminsAsync(resource, userId, now, ct);
        }

        logger.LogInformation(
            "Resource {ResourceId} submitted by {UserId} -> {Status}.",
            resource.Id, userId, resource.Status);
        return resource.Status;
    }

    // Alert every active admin so a resource awaiting review never sits unseen.
    private async Task NotifyAdminsAsync(
        Resource resource, Guid submitterId, DateTimeOffset submittedAt, CancellationToken ct)
    {
        try
        {
            var submitterName = await db.Users
                .Where(u => u.Id == submitterId)
                .Select(u => ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim())
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(submitterName))
                submitterName = "A contributor";

            var adminIds = await db.Users
                .Where(u => (u.ActiveRole == "Admin" || u.ActiveRole == "SuperAdmin")
                            && u.AccountStatus == AccountStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            if (adminIds.Count == 0) return;

            foreach (var adminId in adminIds)
            {
                await notifications.DispatchAsync(
                    adminId,
                    NotificationType.ResourceSubmittedForReview,
                    new NotificationParams
                    {
                        CounterpartyName = submitterName,
                        TitleEn = resource.TitleEn,
                        TitleAr = resource.TitleAr,
                    },
                    deepLink: "/admin/articles",
                    // The submission instant keeps the key unique per submission so
                    // a resubmit after a rejection re-notifies, while a retry inside
                    // the same handler run stays idempotent.
                    idempotencyKey: $"resource-submitted:{resource.Id:N}:{submittedAt.UtcTicks}:{adminId:N}",
                    ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to notify admins of resource {ResourceId} pending review.", resource.Id);
        }
    }
}
