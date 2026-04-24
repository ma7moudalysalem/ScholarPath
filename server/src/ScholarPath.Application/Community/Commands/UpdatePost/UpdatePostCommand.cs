using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Community.Commands.UpdatePost;

public sealed record UpdatePostCommand(
    Guid PostId,
    string? Title,
    string BodyMarkdown) : IRequest<bool>;

public sealed class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
{
    public UpdatePostCommandValidator()
    {
        RuleFor(v => v.PostId).NotEmpty();
        RuleFor(v => v.Title).MaximumLength(200);
        RuleFor(v => v.BodyMarkdown).NotEmpty().MaximumLength(10000);
    }
}

public sealed class UpdatePostCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdatePostCommand, bool>
{
    public async Task<bool> Handle(UpdatePostCommand request, CancellationToken ct)
    {
        var post = await db.ForumPosts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ForumPost), request.PostId);

        if (post.AuthorId != currentUser.UserId)
            throw new ForbiddenAccessException();

        if (post.ParentPostId == null && string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Title", "Root posts must have a title.") });
        }

        var sanitizer = new Ganss.Xss.HtmlSanitizer();

        if (post.ParentPostId == null && !string.IsNullOrEmpty(request.Title))
        {
            post.Title = sanitizer.Sanitize(request.Title);
        }

        post.BodyMarkdown = sanitizer.Sanitize(request.BodyMarkdown);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
