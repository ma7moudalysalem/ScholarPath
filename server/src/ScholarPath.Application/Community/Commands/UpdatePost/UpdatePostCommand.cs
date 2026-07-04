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
    // Bilingual root-post fields. For a REPLY, only BodyEn is used (single body);
    // TitleEn/TitleAr/BodyAr are ignored.
    string? TitleEn,
    string? TitleAr,
    string BodyEn,
    string? BodyAr,
    IReadOnlyList<string>? Tags = null) : IRequest<bool>;

public sealed class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
{
    public UpdatePostCommandValidator()
    {
        RuleFor(v => v.PostId).NotEmpty();
        RuleFor(v => v.TitleEn).MaximumLength(200);
        RuleFor(v => v.TitleAr).MaximumLength(200);
        RuleFor(v => v.BodyEn).NotEmpty().MaximumLength(10000);
        RuleFor(v => v.BodyAr).MaximumLength(10000);
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
        var sanitizer = new Ganss.Xss.HtmlSanitizer();

        if (isRoot)
        {
            // Root posts are bilingual — require both languages for title + body.
            if (string.IsNullOrWhiteSpace(request.TitleEn)
                || string.IsNullOrWhiteSpace(request.TitleAr)
                || string.IsNullOrWhiteSpace(request.BodyAr))
            {
                throw new FluentValidation.ValidationException(
                    new[] { new FluentValidation.Results.ValidationFailure(
                        "Title", "Root posts require an English and Arabic title and body.") });
            }

            var titleEn = sanitizer.Sanitize(request.TitleEn!);
            var titleAr = sanitizer.Sanitize(request.TitleAr!);
            var bodyEn = sanitizer.Sanitize(request.BodyEn);
            var bodyAr = sanitizer.Sanitize(request.BodyAr!);

            post.TitleEn = titleEn;
            post.TitleAr = titleAr;
            post.BodyEn = bodyEn;
            post.BodyAr = bodyAr;
            // Keep the legacy single-language columns mirrored to the English side.
            post.Title = titleEn;
            post.BodyMarkdown = bodyEn;
        }
        else
        {
            // Replies stay single-language.
            var body = sanitizer.Sanitize(request.BodyEn);
            post.BodyEn = body;
            post.BodyMarkdown = body;
        }

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
