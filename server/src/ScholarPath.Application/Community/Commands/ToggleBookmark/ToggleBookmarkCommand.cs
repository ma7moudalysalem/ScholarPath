using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Community.Commands.ToggleBookmark;

[Auditable(AuditAction.Update, "ForumBookmark",
    TargetIdProperty = nameof(PostId),
    SummaryTemplate = "Toggled bookmark on post {PostId}")]
public sealed record ToggleBookmarkCommand(Guid PostId) : IRequest<bool>;

public sealed class ToggleBookmarkCommandValidator : AbstractValidator<ToggleBookmarkCommand>
{
    public ToggleBookmarkCommandValidator()
    {
        RuleFor(v => v.PostId).NotEmpty();
    }
}

/// <summary>
/// Toggle: returns true when the post is now bookmarked, false when the
/// bookmark was removed. Only Students can bookmark; only root posts are
/// bookmarkable.
/// </summary>
public sealed class ToggleBookmarkCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<ToggleBookmarkCommand, bool>
{
    public async Task<bool> Handle(ToggleBookmarkCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Student"))
            throw new ForbiddenAccessException("Only students can bookmark community posts.");

        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException();

        var post = await db.ForumPosts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ForumPost), request.PostId);

        if (post.ParentPostId != null)
            throw new ConflictException("Only root posts can be bookmarked.");

        var existing = await db.ForumBookmarks
            .FirstOrDefaultAsync(b => b.ForumPostId == request.PostId && b.UserId == userId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            db.ForumBookmarks.Remove(existing);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return false;
        }

        db.ForumBookmarks.Add(new ForumBookmark
        {
            ForumPostId = request.PostId,
            UserId = userId,
        });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
