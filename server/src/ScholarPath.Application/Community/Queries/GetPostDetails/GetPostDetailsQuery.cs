using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Community.DTOs;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Community.Queries.GetPostDetails;

public sealed record GetPostDetailsQuery(Guid Id) : IRequest<ForumThreadDto>;

public sealed class GetPostDetailsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPostDetailsQuery, ForumThreadDto>
{
    public async Task<ForumThreadDto> Handle(GetPostDetailsQuery request, CancellationToken ct)
    {
        var post = await db.ForumPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted && !p.IsAutoHidden, ct)
            ?? throw new NotFoundException(nameof(ForumPost), request.Id);

        var replies = await db.ForumPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => p.ParentPostId == request.Id && !p.IsDeleted && !p.IsAutoHidden)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ForumPostDto(
                p.Id,
                p.AuthorId,
                p.Author!.FullName ?? "Anonymous",
                p.CategoryId,
                p.Title,
                p.BodyMarkdown,
                p.UpvoteCount,
                p.DownvoteCount,
                p.ReplyCount,
                p.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new ForumThreadDto(
            new ForumPostDto(
                post.Id,
                post.AuthorId,
                post.Author!.FullName ?? "Anonymous",
                post.CategoryId,
                post.Title,
                post.BodyMarkdown,
                post.UpvoteCount,
                post.DownvoteCount,
                post.ReplyCount,
                post.CreatedAt),
            replies);
    }
}
