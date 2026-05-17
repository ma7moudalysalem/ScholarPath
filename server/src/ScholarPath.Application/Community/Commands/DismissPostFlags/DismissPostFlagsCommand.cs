using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Community.Commands.DismissPostFlags;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Admin dismisses the flags on a post ("keep"): every flag is invalidated, the
/// auto-hide is lifted, and the post returns to <c>Visible</c>. Admin-only.
/// </summary>
[Auditable(AuditAction.Moderated, "ForumPost",
    TargetIdProperty = nameof(PostId),
    SummaryTemplate = "Dismissed flags on forum post {PostId}")]
public sealed record DismissPostFlagsCommand(Guid PostId) : IRequest<bool>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class DismissPostFlagsCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<DismissPostFlagsCommandHandler> logger)
    : IRequestHandler<DismissPostFlagsCommand, bool>
{
    public async Task<bool> Handle(DismissPostFlagsCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only an administrator can moderate the community.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var post = await db.ForumPosts
            .Include(p => p.Flags)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ForumPost), request.PostId);

        foreach (var flag in post.Flags)
            flag.IsValid = false;

        post.FlagCount = 0;
        post.IsAutoHidden = false;
        post.AutoHiddenAt = null;
        post.ModerationStatus = PostModerationStatus.Visible;
        post.ModeratedByAdminId = adminId;
        post.ModeratedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Flags dismissed on post {PostId} by {AdminId}.", post.Id, adminId);
        return true;
    }
}
