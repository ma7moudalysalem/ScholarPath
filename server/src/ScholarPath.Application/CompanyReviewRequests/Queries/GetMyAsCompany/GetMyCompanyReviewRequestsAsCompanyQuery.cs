using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Common;
using ScholarPath.Application.CompanyReviewRequests.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.CompanyReviewRequests.Queries.GetMyAsCompany;

/// <summary>
/// Lists CompanyReviewRequests addressed TO the authenticated Company, newest
/// first. Drives the company "Incoming review requests" page where Accept,
/// Reject, and Complete actions live.
/// </summary>
public sealed record GetMyCompanyReviewRequestsAsCompanyQuery()
    : IRequest<IReadOnlyList<CompanyReviewRequestDto>>;

public sealed class GetMyCompanyReviewRequestsAsCompanyQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyCompanyReviewRequestsAsCompanyQuery,
                      IReadOnlyList<CompanyReviewRequestDto>>
{
    public async Task<IReadOnlyList<CompanyReviewRequestDto>> Handle(
        GetMyCompanyReviewRequestsAsCompanyQuery request, CancellationToken ct)
    {
        var companyId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await db.CompanyReviewRequests
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(CompanyReviewRequestMapper.Projection)
            .ToListAsync(ct);
    }
}
