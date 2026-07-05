using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Queries.GetResourceProgressDetail;

/// <summary>
/// The authenticated student's progress for a single resource, including the ids of the
/// chapters they have already completed so the detail page can render per-chapter state
/// and a completion bar (PB-009 AC#6).
/// </summary>
public sealed record GetResourceProgressDetailQuery(Guid ResourceId)
    : IRequest<ResourceProgressDetailDto>;

public sealed class GetResourceProgressDetailQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetResourceProgressDetailQuery, ResourceProgressDetailDto>
{
    public async Task<ResourceProgressDetailDto> Handle(
        GetResourceProgressDetailQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var totalChapters = await db.ResourceChapters
            .CountAsync(c => c.ResourceId == request.ResourceId, ct);

        var progress = await db.ResourceProgress
            .AsNoTracking()
            .Include(p => p.ChapterProgress)
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.ResourceId == request.ResourceId, ct);

        var completedChapterIds = progress is null
            ? new List<Guid>()
            : progress.ChapterProgress
                .Where(cp => cp.IsCompleted)
                .Select(cp => cp.ResourceChildId)
                .ToList();

        return new ResourceProgressDetailDto(
            request.ResourceId,
            completedChapterIds.Count,
            totalChapters,
            completedChapterIds);
    }
}
