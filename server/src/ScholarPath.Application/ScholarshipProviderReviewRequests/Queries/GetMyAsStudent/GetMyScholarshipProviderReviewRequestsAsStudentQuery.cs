using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Common;
using ScholarPath.Application.ScholarshipProviderReviewRequests.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Queries.GetMyAsStudent;

/// <summary>
/// Lists ScholarshipProviderReviewRequests raised BY the authenticated Student, newest
/// first. Drives the student "My review requests" page.
/// </summary>
public sealed record GetMyScholarshipProviderReviewRequestsAsStudentQuery()
    : IRequest<IReadOnlyList<ScholarshipProviderReviewRequestDto>>;

public sealed class GetMyScholarshipProviderReviewRequestsAsStudentQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyScholarshipProviderReviewRequestsAsStudentQuery,
                      IReadOnlyList<ScholarshipProviderReviewRequestDto>>
{
    public async Task<IReadOnlyList<ScholarshipProviderReviewRequestDto>> Handle(
        GetMyScholarshipProviderReviewRequestsAsStudentQuery request, CancellationToken ct)
    {
        var studentId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var rows = await db.ScholarshipProviderReviewRequests
            .AsNoTracking()
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(ScholarshipProviderReviewRequestMapper.Projection)
            .ToListAsync(ct);

        // Include the files the student attached so they can see + manage them.
        return await rows.WithDocumentsAsync(db, ct);
    }
}
