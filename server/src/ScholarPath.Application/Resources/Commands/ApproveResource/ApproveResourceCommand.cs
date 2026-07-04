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

namespace ScholarPath.Application.Resources.Commands.ApproveResource;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>Admin approves a pending resource — moves it to <c>Published</c> (PB-009 AC#3).</summary>
[Auditable(AuditAction.Approved, "Resource",
    TargetIdProperty = nameof(ResourceId),
    SummaryTemplate = "Approved resource {ResourceId}")]
public sealed record ApproveResourceCommand(Guid ResourceId) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ApproveResourceCommandValidator : AbstractValidator<ApproveResourceCommand>
{
    public ApproveResourceCommandValidator() => RuleFor(x => x.ResourceId).NotEmpty();
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ApproveResourceCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<ApproveResourceCommandHandler> logger)
    : IRequestHandler<ApproveResourceCommand, bool>
{
    public async Task<bool> Handle(ApproveResourceCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can approve resources.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var resource = await db.Resources
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId, ct)
            ?? throw new NotFoundException(nameof(Resource), request.ResourceId);

        if (resource.Status != ResourceStatus.PendingReview)
            throw new ConflictException("Only a resource pending review can be approved.");

        // Defensive re-check — the resource must still satisfy the publish rules.
        var authorBio = await db.UserProfiles
            .Where(p => p.UserId == resource.AuthorUserId)
            .Select(p => p.Biography)
            .FirstOrDefaultAsync(ct);

        var blockers = ResourcePublishRules.FindBlockers(resource, authorBio);
        if (blockers.Count > 0)
            throw new ConflictException(
                "Resource cannot be published: " + string.Join(" ", blockers));

        var now = DateTimeOffset.UtcNow;
        resource.Status = ResourceStatus.Published;
        resource.PublishedAt ??= now;
        resource.ReviewedAt = now;
        resource.ReviewedByAdminId = adminId;
        resource.RejectionReason = null;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await SafeNotifyAsync(resource, ct);

        logger.LogInformation("Resource {ResourceId} approved by {AdminId}.", resource.Id, adminId);
        return true;
    }

    private async Task SafeNotifyAsync(Resource resource, CancellationToken ct)
    {
        try
        {
            await notifications.DispatchAsync(
                resource.AuthorUserId,
                NotificationType.ResourceApproved,
                new NotificationParams { TitleEn = resource.TitleEn, TitleAr = resource.TitleAr },
                deepLink: $"/student/resources/{resource.Slug}",
                idempotencyKey: $"resource-approved:{resource.Id:N}",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Resource-approved notification failed for {ResourceId}.", resource.Id);
        }
    }
}
