using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Community.DTOs;

namespace ScholarPath.Application.Community.Queries.GetPosts;

public sealed record GetPostsQuery(
    Guid? CategoryId = null,
    string? SearchQuery = null,
    string SortBy = "Newest", // "Newest", "MostVoted"
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ForumPostDto>>;

public sealed class GetPostsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPostsQuery, PagedResult<ForumPostDto>>
{
    public async Task<PagedResult<ForumPostDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        var query = db.ForumPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => p.ParentPostId == null && !p.IsDeleted && !p.IsAutoHidden);

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            query = query.Where(p => p.Title!.Contains(request.SearchQuery) || p.BodyMarkdown.Contains(request.SearchQuery));
        }

        query = request.SortBy switch
        {
            "MostVoted" => query.OrderByDescending(p => p.UpvoteCount - p.DownvoteCount),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

        return new PagedResult<ForumPostDto>(items, request.Page, request.PageSize, total);
    }
}
