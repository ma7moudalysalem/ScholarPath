using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Common;
using ScholarPath.Application.ScholarshipProviderReviewRequests.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Queries.GetMyAsScholarshipProvider;

/// <summary>
/// Lists ScholarshipProviderReviewRequests addressed TO the authenticated ScholarshipProvider, newest
/// first. Drives the company "Incoming review requests" page where Accept,
/// Reject, and Complete actions live.
/// </summary>
public sealed record GetMyScholarshipProviderReviewRequestsAsScholarshipProviderQuery()
    : IRequest<IReadOnlyList<ScholarshipProviderReviewRequestDto>>;

public sealed class GetMyScholarshipProviderReviewRequestsAsScholarshipProviderQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyScholarshipProviderReviewRequestsAsScholarshipProviderQuery,
                      IReadOnlyList<ScholarshipProviderReviewRequestDto>>
{
    public async Task<IReadOnlyList<ScholarshipProviderReviewRequestDto>> Handle(
        GetMyScholarshipProviderReviewRequestsAsScholarshipProviderQuery request, CancellationToken ct)
    {
        var companyId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await db.ScholarshipProviderReviewRequests
            .AsNoTracking()
            .Where(r => r.ScholarshipProviderId == companyId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(ScholarshipProviderReviewRequestMapper.Projection)
            .ToListAsync(ct);
    }
}
