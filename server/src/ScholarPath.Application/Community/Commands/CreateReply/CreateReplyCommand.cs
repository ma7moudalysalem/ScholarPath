using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;


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
        var parent = await db.ForumPosts
            .FirstOrDefaultAsync(p => p.Id == request.ParentPostId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ForumPost), request.ParentPostId);

        var sanitizer = new Ganss.Xss.HtmlSanitizer();

        var reply = new ForumPost
        {
           AuthorId = (currentUser.UserId ?? throw new ForbiddenAccessException()),
           ParentPostId = request.ParentPostId,
            BodyMarkdown = sanitizer.Sanitize(request.BodyMarkdown)
        };

        parent.ReplyCount++;

        db.ForumPosts.Add(reply);

        reply.RaiseDomainEvent(new ScholarPath.Domain.Events.ForumReplyCreatedEvent(reply.Id, request.ParentPostId, (currentUser.UserId ?? throw new ForbiddenAccessException())));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return reply.Id;
    }
}
