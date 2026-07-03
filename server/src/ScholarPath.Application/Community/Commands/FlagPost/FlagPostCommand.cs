using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;


namespace ScholarPath.Application.Community.Commands.FlagPost;

[Auditable(AuditAction.Create, "PostFlag",
    TargetIdProperty = nameof(PostId),
    SummaryTemplate = "Flagged post {PostId}")]
public sealed record FlagPostCommand(
    Guid PostId,
    string Reason,
    string? AdditionalDetails) : IRequest<bool>;

public sealed class FlagPostCommandValidator : AbstractValidator<FlagPostCommand>
{
    public FlagPostCommandValidator()
    {
        RuleFor(v => v.PostId).NotEmpty();
        RuleFor(v => v.Reason).NotEmpty().MaximumLength(200);
        RuleFor(v => v.AdditionalDetails).MaximumLength(1000);
    }
}

public sealed class FlagPostCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<FlagPostCommandHandler> logger)
    : IRequestHandler<FlagPostCommand, bool>
{
    // ForumPost carries a rowversion concurrency token. Flagging does a
    // read-modify-write on the post (FlagCount + auto-hide), so two students
    // flagging the SAME post at nearly the same time collide on the post UPDATE —
    // exactly the moderation scenario where several people report one bad post at
    // once. The losing SaveChanges threw DbUpdateConcurrencyException, which the
    // DbContext override does not translate (it only maps unique-index 2601/2627),
    // so it surfaced as an HTTP 500. Retry the apply on conflict, reloading the
    // post's current state so neither flag is lost and the count stays correct.
    private const int MaxSaveAttempts = 4;

    public async Task<bool> Handle(FlagPostCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Student"))
            throw new ForbiddenAccessException("Only students can report community content.");

        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var post = await db.ForumPosts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ForumPost), request.PostId);

        if (post.AuthorId == userId)
            throw new ConflictException("You cannot flag your own post.");

        // Friendly-path duplicate check; the unique (ForumPostId, FlaggedByUserId)
        // index is the real guard (translated to a Conflict by the DbContext).
        if (await db.ForumFlags
                .AnyAsync(f => f.ForumPostId == request.PostId && f.FlaggedByUserId == userId, ct)
                .ConfigureAwait(false))
            throw new ConflictException("You have already flagged this post.");

        var flag = new ForumFlag
        {
            ForumPostId = request.PostId,
            FlaggedByUserId = userId,
            Reason = request.Reason,
            AdditionalDetails = request.AdditionalDetails,
        };
        db.ForumFlags.Add(flag);

        for (var attempt = 1; ; attempt++)
        {
            // Distinct valid flaggers already persisted (excludes our pending flag,
            // which is a guaranteed-new flagger by the duplicate check + unique index).
            var priorDistinctValidFlags = await db.ForumFlags
                .Where(f => f.ForumPostId == request.PostId && f.IsValid)
                .Select(f => f.FlaggedByUserId)
                .Distinct()
                .CountAsync(ct)
                .ConfigureAwait(false);
            var distinctValidFlags = priorDistinctValidFlags + 1;

            post.FlagCount++;

            // Auto-hide rule: 3 distinct valid flags route the post to moderation.
            post.ClearDomainEvents(); // avoid a duplicate event if a prior attempt retried
            if (distinctValidFlags >= 3 && !post.IsAutoHidden)
            {
                post.IsAutoHidden = true;
                post.AutoHiddenAt = DateTimeOffset.UtcNow;
                post.ModerationStatus = PostModerationStatus.PendingReview;
                post.RaiseDomainEvent(
                    new ScholarPath.Domain.Events.PostAutoHiddenEvent(post.Id, distinctValidFlags));
            }

            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                break;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxSaveAttempts)
            {
                // A concurrent flag bumped the post's rowversion. Refresh the post's
                // current DB values (FlagCount, IsAutoHidden, rowversion) so the next
                // attempt applies on top of the winner instead of failing.
                foreach (var entry in ex.Entries)
                    await entry.ReloadAsync(ct).ConfigureAwait(false);
            }
        }

        // Let the admins know there's reported content awaiting moderation.
        // Best-effort: a notification failure must never break the report.
        await NotifyAdminsAsync(request.PostId, flag.FlaggedByUserId, request.Reason, ct);

        return true;
    }

    // PB-007 — alert every admin so reported content gets a timely moderation
    // decision. Keyed per (post, reporter, admin) so a single report fires once.
    private async Task NotifyAdminsAsync(
        Guid postId, Guid reporterId, string reason, CancellationToken ct)
    {
        try
        {
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
                    NotificationType.ContentReported,
                    new NotificationParams { Reason = reason },
                    deepLink: "/admin/community",
                    idempotencyKey: $"content-reported:{postId:N}:{reporterId:N}:{adminId:N}",
                    ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to notify admins that post {PostId} was reported.", postId);
        }
    }
}
