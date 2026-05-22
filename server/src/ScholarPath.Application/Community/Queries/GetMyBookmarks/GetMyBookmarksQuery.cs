using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Community.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Community.Queries.GetMyBookmarks;

/// <summary>
/// Lists the calling Student's bookmarked root posts, most-recently-bookmarked
/// first. Excludes deleted or auto-hidden posts so revoked content never
/// appears in the list. Student-only.
/// </summary>
public sealed record GetMyBookmarksQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ForumPostDto>>;

public sealed class GetMyBookmarksQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyBookmarksQuery, PagedResult<ForumPostDto>>
{
    public async Task<PagedResult<ForumPostDto>> Handle(GetMyBookmarksQuery request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Student"))
            throw new ForbiddenAccessException("Only students can view bookmarks.");

        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException();

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var baseQuery = db.ForumBookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId
                        && b.ForumPost != null
                        && !b.ForumPost.IsDeleted
                        && !b.ForumPost.IsAutoHidden);

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var rows = await baseQuery
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.ForumPost!.Id,
                b.ForumPost.AuthorId,
                AuthorName = b.ForumPost.Author!.FullName ?? "Anonymous",
                b.ForumPost.CategoryId,
                b.ForumPost.Title,
                b.ForumPost.BodyMarkdown,
                b.ForumPost.UpvoteCount,
                b.ForumPost.DownvoteCount,
                b.ForumPost.ReplyCount,
                b.ForumPost.CreatedAt,
                Tags = b.ForumPost.PostTags.Select(pt => pt.ForumTag!.Slug).OrderBy(s => s).ToList(),
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = rows
            .Select(r => new ForumPostDto(
                r.Id, r.AuthorId, r.AuthorName, r.CategoryId, r.Title, r.BodyMarkdown,
                r.UpvoteCount, r.DownvoteCount, r.ReplyCount, r.CreatedAt,
                r.Tags, true))
            .ToList();

        return new PagedResult<ForumPostDto>(items, page, pageSize, total);
    }
}
