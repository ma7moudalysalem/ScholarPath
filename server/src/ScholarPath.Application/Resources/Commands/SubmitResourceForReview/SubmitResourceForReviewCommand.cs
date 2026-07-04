using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
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

        logger.LogInformation(
            "Resource {ResourceId} submitted by {UserId} -> {Status}.",
            resource.Id, userId, resource.Status);
        return resource.Status;
    }
}
