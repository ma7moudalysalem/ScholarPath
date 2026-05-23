using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Common;
using ScholarPath.Application.CompanyReviewRequests.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.CompanyReviewRequests.Queries.GetMyAsStudent;

/// <summary>
/// Lists CompanyReviewRequests raised BY the authenticated Student, newest
/// first. Drives the student "My review requests" page.
/// </summary>
public sealed record GetMyCompanyReviewRequestsAsStudentQuery()
    : IRequest<IReadOnlyList<CompanyReviewRequestDto>>;

public sealed class GetMyCompanyReviewRequestsAsStudentQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyCompanyReviewRequestsAsStudentQuery,
                      IReadOnlyList<CompanyReviewRequestDto>>
{
    public async Task<IReadOnlyList<CompanyReviewRequestDto>> Handle(
        GetMyCompanyReviewRequestsAsStudentQuery request, CancellationToken ct)
    {
        var studentId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await db.CompanyReviewRequests
            .AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(CompanyReviewRequestMapper.Projection)
            .ToListAsync(ct);
    }
}
