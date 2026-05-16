using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Queries.GetCompanyApplicationDetails;

public sealed class GetCompanyApplicationDetailsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetCompanyApplicationDetailsQuery, CompanyApplicationDetailsDto>
{
    public async Task<CompanyApplicationDetailsDto> Handle(GetCompanyApplicationDetailsQuery request, CancellationToken ct)
    {
        var application = await db.Applications
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Scholarship)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ApplicationTracker), request.ApplicationId);

        // Verify company owns the scholarship
        if (application.Scholarship == null || application.Scholarship.OwnerCompanyId != currentUser.UserId)
        {
            throw new ForbiddenAccessException();
        }

        return new CompanyApplicationDetailsDto(
            application.Id,
            application.StudentId,
            application.Student?.FullName ?? "Unknown",
            application.ScholarshipId,
            application.Scholarship.TitleEn,
            application.Status,
            application.SubmittedAt,
            application.FormDataJson,
            application.AttachedDocumentsJson
        );
    }
}
