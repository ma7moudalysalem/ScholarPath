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

namespace ScholarPath.Application.Resources.Commands.RejectResource;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>Admin rejects a pending resource — sends it back to <c>Draft</c> with a reason.</summary>
[Auditable(AuditAction.Rejected, "Resource",
    TargetIdProperty = nameof(ResourceId),
    SummaryTemplate = "Rejected resource {ResourceId}")]
public sealed record RejectResourceCommand(Guid ResourceId, string RejectionReason) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class RejectResourceCommandValidator : AbstractValidator<RejectResourceCommand>
{
    public RejectResourceCommandValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MaximumLength(2000);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class RejectResourceCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<RejectResourceCommandHandler> logger)
    : IRequestHandler<RejectResourceCommand, bool>
{
    public async Task<bool> Handle(RejectResourceCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can reject resources.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var resource = await db.Resources
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId, ct)
            ?? throw new NotFoundException(nameof(Resource), request.ResourceId);

        if (resource.Status != ResourceStatus.PendingReview)
            throw new ConflictException("Only a resource pending review can be rejected.");

        resource.Status = ResourceStatus.Draft;
        resource.RejectionReason = request.RejectionReason;
        resource.ReviewedAt = DateTimeOffset.UtcNow;
        resource.ReviewedByAdminId = adminId;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await SafeNotifyAsync(resource, request.RejectionReason, ct);

        logger.LogInformation("Resource {ResourceId} rejected by {AdminId}.", resource.Id, adminId);
        return true;
    }

    private async Task SafeNotifyAsync(Resource resource, string reason, CancellationToken ct)
    {
        try
        {
            await notifications.DispatchAsync(
                resource.AuthorUserId,
                NotificationType.ResourceRejected,
                new NotificationParams { TitleEn = resource.TitleEn, TitleAr = resource.TitleAr, Reason = reason },
                deepLink: null,
                idempotencyKey: null,
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Resource-rejected notification failed for {ResourceId}.", resource.Id);
        }
    }
}
