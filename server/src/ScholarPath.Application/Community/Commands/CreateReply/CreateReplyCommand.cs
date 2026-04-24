using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Community.Commands.CreateReply;

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
            AuthorId = currentUser.UserId,
            ParentPostId = request.ParentPostId,
            BodyMarkdown = sanitizer.Sanitize(request.BodyMarkdown)
        };

        parent.ReplyCount++;

        db.ForumPosts.Add(reply);
        
        reply.RaiseDomainEvent(new ScholarPath.Domain.Events.ForumReplyCreatedEvent(reply.Id, request.ParentPostId, currentUser.UserId));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return reply.Id;
    }
}
