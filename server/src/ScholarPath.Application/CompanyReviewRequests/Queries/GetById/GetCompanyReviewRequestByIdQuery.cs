using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Common;
using ScholarPath.Application.CompanyReviewRequests.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.CompanyReviewRequests.Queries.GetById;

public sealed record GetCompanyReviewRequestByIdQuery(Guid Id)
    : IRequest<CompanyReviewRequestDto>;

public sealed class GetCompanyReviewRequestByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetCompanyReviewRequestByIdQuery, CompanyReviewRequestDto>
{
    public async Task<CompanyReviewRequestDto> Handle(
        GetCompanyReviewRequestByIdQuery request, CancellationToken ct)
    {
        var dto = await db.CompanyReviewRequests
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .Select(CompanyReviewRequestMapper.Projection)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyReviewRequest), request.Id);

        // Authorization — the participants and admins are the only legitimate
        // readers. A Company seeing another Company's payment numbers would be
        // a confidentiality break.
        if (currentUser.UserId != dto.StudentId
            && currentUser.UserId != dto.CompanyId
            && !currentUser.IsInRole("Admin"))
        {
            throw new ForbiddenAccessException();
        }

        return dto;
    }
}
