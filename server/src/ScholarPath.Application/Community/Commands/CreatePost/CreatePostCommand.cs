using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Community.Tags;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Community.Commands.CreatePost;

[Auditable(AuditAction.Create, "ForumPost",
    SummaryTemplate = "Created forum post")]
public sealed record CreatePostCommand(
    Guid CategoryId,
    string TitleEn,
    string TitleAr,
    string BodyEn,
    string BodyAr,
    IReadOnlyList<string>? Tags = null) : IRequest<Guid>;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(v => v.CategoryId).NotEmpty();
        // Community posts are bilingual — both languages required, like scholarships.
        RuleFor(v => v.TitleEn).NotEmpty().MaximumLength(200);
        RuleFor(v => v.TitleAr).NotEmpty().MaximumLength(200);
        RuleFor(v => v.BodyEn).NotEmpty().MaximumLength(10000);
        RuleFor(v => v.BodyAr).NotEmpty().MaximumLength(10000);
        RuleFor(v => v.Tags!)
            .Must(tags => tags == null || tags.Count <= TagPolicy.MaxTagsPerPost)
            .WithMessage($"At most {TagPolicy.MaxTagsPerPost} tags are allowed.")
            .When(v => v.Tags != null);
    }
}

public sealed class CreatePostCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        // Only Students can create community posts. Consultants/Companies must
        // not produce content here; Admin moderation goes through the admin
        // routes, not this command.
        if (!currentUser.IsInRole("Student"))
            throw new ForbiddenAccessException("Only students can create community posts.");

        var authorId = currentUser.UserId
            ?? throw new ForbiddenAccessException();

        var sanitizer = new Ganss.Xss.HtmlSanitizer();

        var titleEn = sanitizer.Sanitize(request.TitleEn);
        var titleAr = sanitizer.Sanitize(request.TitleAr);
        var bodyEn = sanitizer.Sanitize(request.BodyEn);
        var bodyAr = sanitizer.Sanitize(request.BodyAr);

        var post = new ForumPost
        {
            AuthorId = authorId,
            CategoryId = request.CategoryId,
            TitleEn = titleEn,
            TitleAr = titleAr,
            BodyEn = bodyEn,
            BodyAr = bodyAr,
            // Mirror the English side into the legacy single-language columns so
            // search and any legacy reader keep working (BodyMarkdown is NOT NULL).
            Title = titleEn,
            BodyMarkdown = bodyEn,
        };

        db.ForumPosts.Add(post);

        await TagPolicy.AttachTagsAsync(db, post, request.Tags, ct).ConfigureAwait(false);

        // Raise BEFORE save so the dispatcher (running after SaveChanges) picks
        // it up — otherwise the SignalR broadcast never fires.
        post.RaiseDomainEvent(new ForumPostCreatedEvent(post.Id, authorId, request.CategoryId));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return post.Id;
    }
}
