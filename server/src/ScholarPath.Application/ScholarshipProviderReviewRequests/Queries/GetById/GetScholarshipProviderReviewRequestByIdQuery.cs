using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Common;
using ScholarPath.Application.ScholarshipProviderReviewRequests.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Queries.GetById;

public sealed record GetScholarshipProviderReviewRequestByIdQuery(Guid Id)
    : IRequest<ScholarshipProviderReviewRequestDto>;

public sealed class GetScholarshipProviderReviewRequestByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetScholarshipProviderReviewRequestByIdQuery, ScholarshipProviderReviewRequestDto>
{
    public async Task<ScholarshipProviderReviewRequestDto> Handle(
        GetScholarshipProviderReviewRequestByIdQuery request, CancellationToken ct)
    {
        var dto = await db.ScholarshipProviderReviewRequests
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .Select(ScholarshipProviderReviewRequestMapper.Projection)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ScholarshipProviderReviewRequest), request.Id);

        // Authorization — the participants and admins are the only legitimate
        // readers. A ScholarshipProvider seeing another ScholarshipProvider's payment numbers would be
        // a confidentiality break.
        if (currentUser.UserId != dto.StudentId
            && currentUser.UserId != dto.ScholarshipProviderId
            && !currentUser.IsAdminOrSuperAdmin())
        {
            throw new ForbiddenAccessException();
        }

        return dto;
    }
}
