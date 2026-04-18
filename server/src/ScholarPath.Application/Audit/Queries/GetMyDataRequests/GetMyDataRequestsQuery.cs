using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Audit.Queries.GetMyDataRequests;

public sealed record GetMyDataRequestsQuery : IRequest<IReadOnlyList<DataRequestDto>>;

public sealed class GetMyDataRequestsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyDataRequestsQuery, IReadOnlyList<DataRequestDto>>
{
    public async Task<IReadOnlyList<DataRequestDto>> Handle(GetMyDataRequestsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var rows = await db.UserDataRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new DataRequestDto(
                r.Id, r.Type, r.Status, r.RequestedAt, r.ScheduledProcessAt,
                r.CompletedAt, r.CancelledAt, r.DownloadUrl, r.DownloadExpiresAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows;
    }
}
