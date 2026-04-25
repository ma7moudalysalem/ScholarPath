using FluentValidation;
using MediatR;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Domain.Events;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;
using ScholarPath.Application.Common.Exceptions;


namespace ScholarPath.Application.Community.Commands.CreatePost;
[Auditable(AuditAction.Create, "ForumPost",
    SummaryTemplate = "Created forum post")]
public sealed record CreatePostCommand(
    Guid CategoryId,
    string Title,
    string BodyMarkdown) : IRequest<Guid>;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(v => v.CategoryId).NotEmpty();
        RuleFor(v => v.Title).NotEmpty().MaximumLength(200);
        RuleFor(v => v.BodyMarkdown).NotEmpty().MaximumLength(10000);
    }
}

public sealed class CreatePostCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var sanitizer = new Ganss.Xss.HtmlSanitizer();

        var post = new ForumPost
        {
            AuthorId = (currentUser.UserId ?? throw new ForbiddenAccessException()),
            CategoryId = request.CategoryId,
            Title = sanitizer.Sanitize(request.Title),
            BodyMarkdown = sanitizer.Sanitize(request.BodyMarkdown) // T-005: Sanitize content
        };

        db.ForumPosts.Add(post);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // T-004 Broadcast new post via CommunityHub will be triggered by an event
        post.RaiseDomainEvent(new ScholarPath.Domain.Events.ForumPostCreatedEvent(post.Id, (currentUser.UserId ?? throw new ForbiddenAccessException()), request.CategoryId));

        return post.Id;
    }
}
