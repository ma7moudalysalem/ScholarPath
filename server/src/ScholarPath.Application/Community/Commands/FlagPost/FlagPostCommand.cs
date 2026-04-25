using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
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
    ICurrentUserService currentUser)
    : IRequestHandler<FlagPostCommand, bool>
{
    public async Task<bool> Handle(FlagPostCommand request, CancellationToken ct)
    {
        var post = await db.ForumPosts
            .Include(p => p.Flags)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ForumPost), request.PostId);

        if (post.AuthorId == currentUser.UserId)
            throw new ConflictException("You cannot flag your own post.");

        if (post.Flags.Any(f => f.FlaggedByUserId == currentUser.UserId))
            throw new ConflictException("You have already flagged this post.");

        var flag = new ForumFlag
        {
            ForumPostId = request.PostId,
            FlaggedByUserId = (currentUser.UserId ?? throw new ForbiddenAccessException()),
            Reason = request.Reason,
            AdditionalDetails = request.AdditionalDetails
        };

        post.Flags.Add(flag);
        post.FlagCount++;

        // Auto-hide rule: 3 distinct valid flags
        var distinctValidFlags = post.Flags.Where(f => f.IsValid).Select(f => f.FlaggedByUserId).Distinct().Count();
        if (distinctValidFlags >= 3 && !post.IsAutoHidden)
        {
            post.IsAutoHidden = true;
            post.AutoHiddenAt = DateTimeOffset.UtcNow;

            // Raise event that post was auto-hidden
            post.RaiseDomainEvent(new ScholarPath.Domain.Events.PostAutoHiddenEvent(post.Id, distinctValidFlags));
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
