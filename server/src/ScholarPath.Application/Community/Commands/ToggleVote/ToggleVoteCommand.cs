using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Community.Commands.ToggleVote;

[Auditable(AuditAction.Update, "PostVote",
    TargetIdProperty = nameof(PostId),
    SummaryTemplate = "Toggled vote on post {PostId}")]
public sealed record ToggleVoteCommand(
    Guid PostId,
    VoteType VoteType) : IRequest<bool>;

public sealed class ToggleVoteCommandValidator : AbstractValidator<ToggleVoteCommand>
{
    public ToggleVoteCommandValidator()
    {
        RuleFor(v => v.PostId).NotEmpty();
        RuleFor(v => v.VoteType).IsInEnum();
    }
}

public sealed class ToggleVoteCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<ToggleVoteCommand, bool>
{
    public async Task<bool> Handle(ToggleVoteCommand request, CancellationToken ct)
    {
        var post = await db.ForumPosts
            .Include(p => p.Votes.Where(v => v.UserId == currentUser.UserId))
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ForumPost), request.PostId);

        if (post.AuthorId == currentUser.UserId)
            throw new ConflictException("You cannot vote on your own post.");

        var existingVote = post.Votes.FirstOrDefault(v => v.UserId == currentUser.UserId);

        if (existingVote != null)
        {
            // If same vote type, toggle off (remove vote)
            if (existingVote.VoteType == request.VoteType)
            {
                db.ForumVotes.Remove(existingVote);
                if (request.VoteType == VoteType.Up) post.UpvoteCount--;
                else post.DownvoteCount--;
            }
            // If different vote type, swap
            else
            {
                if (existingVote.VoteType == VoteType.Up)
                {
                    post.UpvoteCount--;
                    post.DownvoteCount++;
                }
                else
                {
                    post.DownvoteCount--;
                    post.UpvoteCount++;
                }
                existingVote.VoteType = request.VoteType;
                existingVote.VotedAt = DateTimeOffset.UtcNow;
            }
        }
        else
        {
            // Add new vote
            var vote = new ForumVote
            {
                ForumPostId = request.PostId,
                UserId = (currentUser.UserId ?? throw new ForbiddenAccessException()),
                VoteType = request.VoteType
            };
            db.ForumVotes.Add(vote);

            if (request.VoteType == VoteType.Up) post.UpvoteCount++;
            else post.DownvoteCount++;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
