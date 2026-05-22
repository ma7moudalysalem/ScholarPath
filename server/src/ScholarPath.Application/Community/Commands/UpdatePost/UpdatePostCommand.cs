using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Community.Tags;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Community.Commands.UpdatePost;

[Auditable(AuditAction.Update, "ForumPost",
    TargetIdProperty = nameof(PostId),
    SummaryTemplate = "Updated forum post {PostId}")]
public sealed record UpdatePostCommand(
    Guid PostId,
    string? Title,
    string BodyMarkdown,
    IReadOnlyList<string>? Tags = null) : IRequest<bool>;

public sealed class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
{
    public UpdatePostCommandValidator()
    {
        RuleFor(v => v.PostId).NotEmpty();
        RuleFor(v => v.Title).MaximumLength(200);
        RuleFor(v => v.BodyMarkdown).NotEmpty().MaximumLength(10000);
        RuleFor(v => v.Tags!)
            .Must(tags => tags == null || tags.Count <= TagPolicy.MaxTagsPerPost)
            .WithMessage($"At most {TagPolicy.MaxTagsPerPost} tags are allowed.")
            .When(v => v.Tags != null);
    }
}

public sealed class UpdatePostCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdatePostCommand, bool>
{
    public async Task<bool> Handle(UpdatePostCommand request, CancellationToken ct)
    {
        // Only Students can update community posts (they're the only role allowed
        // to author one in the first place). Admin moderation does not go
        // through this command.
        if (!currentUser.IsInRole("Student"))
            throw new ForbiddenAccessException("Only students can edit community posts.");

        var post = await db.ForumPosts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ForumPost), request.PostId);

        if (post.AuthorId != currentUser.UserId)
            throw new ForbiddenAccessException();

        var isRoot = post.ParentPostId == null;

        if (isRoot && string.IsNullOrWhiteSpace(request.Title))
        {
            throw new FluentValidation.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure("Title", "Root posts must have a title.") });
        }

        var sanitizer = new Ganss.Xss.HtmlSanitizer();

        if (isRoot && !string.IsNullOrEmpty(request.Title))
        {
            post.Title = sanitizer.Sanitize(request.Title);
        }

        post.BodyMarkdown = sanitizer.Sanitize(request.BodyMarkdown);

        // Tags only apply to root posts. For replies the field is ignored
        // — they're threaded under a tagged root anyway.
        if (isRoot && request.Tags is not null)
        {
            await TagPolicy.AttachTagsAsync(db, post, request.Tags, ct).ConfigureAwait(false);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
