using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Community.Commands.CreateReply;

[Auditable(AuditAction.Create, "ForumPost",
    TargetIdProperty = nameof(ParentPostId),
    SummaryTemplate = "Created reply to forum post {ParentPostId}")]
public sealed record CreateReplyCommand(
    Guid ParentPostId,
    string BodyMarkdown) : IRequest<Guid>;

public sealed class CreateReplyCommandValidator : AbstractValidator<CreateReplyCommand>
{
    public CreateReplyCommandValidator()
    {
        RuleFor(v => v.ParentPostId).NotEmpty();
        RuleFor(v => v.BodyMarkdown).NotEmpty().MaximumLength(10000);
    }
}

public sealed class CreateReplyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateReplyCommand, Guid>
{
    public async Task<Guid> Handle(CreateReplyCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Student"))
            throw new ForbiddenAccessException("Only students can reply in the community.");

        var authorId = currentUser.UserId
            ?? throw new ForbiddenAccessException();

        var parent = await db.ForumPosts
            .FirstOrDefaultAsync(p => p.Id == request.ParentPostId && !p.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ForumPost), request.ParentPostId);

        // FR-MSG-29: a mutual block prevents either party from replying to the
        // other's community posts.
        if (await CommunityBlockFilter.AreBlockedAsync(db, authorId, parent.AuthorId, ct).ConfigureAwait(false))
        {
            throw new ConflictException("You cannot reply to this post because a block is in place.");
        }

        var sanitizer = new Ganss.Xss.HtmlSanitizer();

        var body = sanitizer.Sanitize(request.BodyMarkdown);
        var reply = new ForumPost
        {
            AuthorId = authorId,
            ParentPostId = request.ParentPostId,
            BodyMarkdown = body,
            // Replies are single-language; mirror into BodyEn so the bilingual
            // projection (which reads BodyEn with a fallback) shows the text.
            BodyEn = body,
        };

        parent.ReplyCount++;

        db.ForumPosts.Add(reply);

        reply.RaiseDomainEvent(new ForumReplyCreatedEvent(reply.Id, request.ParentPostId, authorId));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return reply.Id;
    }
}
