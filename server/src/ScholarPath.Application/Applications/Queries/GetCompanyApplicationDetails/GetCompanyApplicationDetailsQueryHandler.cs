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

        var documents = await db.Documents
            .AsNoTracking()
            .Where(d => d.ApplicationTrackerId == application.Id && !d.IsDeleted)
            .Select(d => new CompanyDocumentInfo(d.Id, d.FileName, d.ContentType, d.SizeBytes))
            .ToListAsync(ct);

        return new CompanyApplicationDetailsDto(
            application.Id,
            application.StudentId,
            application.Student?.FullName ?? "Unknown",
            // Company-side queries always operate on platform scholarships; the
            // guard above guarantees Scholarship is non-null, so ScholarshipId is set.
            application.ScholarshipId!.Value,
            application.Scholarship.TitleEn,
            application.Status,
            application.SubmittedAt,
            application.FormDataJson,
            application.AttachedDocumentsJson,
            documents
        );
    }
}
