using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Community.Commands.DeletePost;

[Auditable(AuditAction.Delete, "ForumPost",
    TargetIdProperty = nameof(PostId),
    SummaryTemplate = "Deleted forum post {PostId}")]
public sealed record DeletePostCommand(
    Guid PostId) : IRequest<bool>;

public sealed class DeletePostCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<DeletePostCommand, bool>
{
    public async Task<bool> Handle(DeletePostCommand request, CancellationToken ct)
    {
        var post = await db.ForumPosts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ForumPost), request.PostId);

        // Allow author or admin to delete
        var isAdmin = currentUser.Roles?.Contains("Admin") == true;
        if (post.AuthorId != currentUser.UserId && !isAdmin)
            throw new ForbiddenAccessException();

        post.IsDeleted = true;
        post.DeletedAt = DateTimeOffset.UtcNow;
        post.DeletedByUserId = currentUser.UserId;

        // FR-ADM-05: an admin removing someone else's post is a moderation
        // decision — stamp the moderator + set the terminal Removed status so the
        // removal is traceable in the post's moderation history (not just the audit
        // log). A self-delete by the author leaves the moderation fields untouched.
        if (isAdmin && post.AuthorId != currentUser.UserId)
        {
            post.ModeratedByAdminId = currentUser.UserId;
            post.ModeratedAt = DateTimeOffset.UtcNow;
            post.ModerationStatus = PostModerationStatus.Removed;
        }

        if (post.ParentPostId.HasValue)
        {
            var parent = await db.ForumPosts.FirstOrDefaultAsync(p => p.Id == post.ParentPostId, ct);
            if (parent != null)
            {
                parent.ReplyCount = Math.Max(0, parent.ReplyCount - 1);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
