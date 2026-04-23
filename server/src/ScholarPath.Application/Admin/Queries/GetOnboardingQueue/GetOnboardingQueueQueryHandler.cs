using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Queries.GetOnboardingQueue;

public sealed class GetOnboardingQueueQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOnboardingQueueQuery, PagedResult<OnboardingRequestRow>>
{
    public async Task<PagedResult<OnboardingRequestRow>> Handle(GetOnboardingQueueQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var q = db.Users
            .AsNoTracking()
            .Where(u => u.AccountStatus == AccountStatus.PendingApproval);

        var total = await q.CountAsync(ct).ConfigureAwait(false);

        var rows = await q
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new OnboardingRequestRow(
                u.Id,
                u.Email!,
                (u.FirstName + " " + u.LastName).Trim(),
                u.AccountStatus,
                u.CreatedAt,
                u.ActiveRole))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<OnboardingRequestRow>(rows, page, pageSize, total);
    }
}
