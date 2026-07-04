using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Community.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Community.Queries.GetPostDetails;

public sealed record GetPostDetailsQuery(Guid Id) : IRequest<ForumThreadDto>;

public sealed class GetPostDetailsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetPostDetailsQuery, ForumThreadDto>
{
    public async Task<ForumThreadDto> Handle(GetPostDetailsQuery request, CancellationToken ct)
    {
        var currentUserId = currentUser.UserId;

        var post = await db.ForumPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => p.Id == request.Id && !p.IsDeleted && !p.IsAutoHidden)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                AuthorName = p.Author!.FullName ?? "Anonymous",
                p.CategoryId,
                p.Title,
                p.BodyMarkdown,
                p.TitleEn,
                p.TitleAr,
                p.BodyEn,
                p.BodyAr,
                p.UpvoteCount,
                p.DownvoteCount,
                p.ReplyCount,
                p.CreatedAt,
                Tags = p.PostTags.Select(pt => pt.ForumTag!.Slug).OrderBy(s => s).ToList(),
                IsBookmarked = currentUserId != null && p.Bookmarks.Any(b => b.UserId == currentUserId),
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ForumPost), request.Id);

        var repliesQuery = db.ForumPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => p.ParentPostId == request.Id && !p.IsDeleted && !p.IsAutoHidden);

        // Personal block: drop replies authored by anyone the current user blocked.
        if (currentUserId is Guid blockerId)
        {
            repliesQuery = repliesQuery.Where(p => !db.UserBlocks.Any(
                b => b.BlockerId == blockerId && b.BlockedUserId == p.AuthorId));
        }

        var replyRows = await repliesQuery
            .OrderBy(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                AuthorName = p.Author!.FullName ?? "Anonymous",
                p.CategoryId,
                p.Title,
                p.BodyMarkdown,
                p.TitleEn,
                p.TitleAr,
                p.BodyEn,
                p.BodyAr,
                p.UpvoteCount,
                p.DownvoteCount,
                p.ReplyCount,
                p.CreatedAt,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var replies = replyRows
            .Select(r => new ForumPostDto(
                r.Id, r.AuthorId, r.AuthorName, r.CategoryId, r.Title, r.BodyMarkdown,
                r.UpvoteCount, r.DownvoteCount, r.ReplyCount, r.CreatedAt,
                Array.Empty<string>(), false,
                r.TitleEn, r.TitleAr, r.BodyEn ?? r.BodyMarkdown, r.BodyAr))
            .ToList();

        return new ForumThreadDto(
            new ForumPostDto(
                post.Id, post.AuthorId, post.AuthorName, post.CategoryId, post.Title, post.BodyMarkdown,
                post.UpvoteCount, post.DownvoteCount, post.ReplyCount, post.CreatedAt,
                post.Tags, post.IsBookmarked,
                post.TitleEn, post.TitleAr, post.BodyEn ?? post.BodyMarkdown, post.BodyAr),
            replies);
    }
}
