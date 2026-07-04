using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Queries.GetScholarshipProviderApplicationDetails;

public sealed class GetScholarshipProviderApplicationDetailsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetScholarshipProviderApplicationDetailsQuery, ScholarshipProviderApplicationDetailsDto>
{
    public async Task<ScholarshipProviderApplicationDetailsDto> Handle(GetScholarshipProviderApplicationDetailsQuery request, CancellationToken ct)
    {
        var application = await db.Applications
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Scholarship)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ApplicationTracker), request.ApplicationId);

        // Verify company owns the scholarship
        if (application.Scholarship == null || application.Scholarship.OwnerScholarshipProviderId != currentUser.UserId)
        {
            throw new ForbiddenAccessException();
        }

        // A provider must never see a student's unsubmitted Draft (in-progress form
        // + uploaded documents). Treat it as not-found to avoid confirming it exists.
        if (application.Status == Domain.Enums.ApplicationStatus.Draft)
        {
            throw new NotFoundException(nameof(Domain.Entities.ApplicationTracker), request.ApplicationId);
        }

        var documents = await db.Documents
            .AsNoTracking()
            .Where(d => d.ApplicationTrackerId == application.Id && !d.IsDeleted)
            .Select(d => new ScholarshipProviderDocumentInfo(d.Id, d.FileName, d.ContentType, d.SizeBytes))
            .ToListAsync(ct);

        return new ScholarshipProviderApplicationDetailsDto(
            application.Id,
            application.StudentId,
            application.Student?.FullName ?? "Unknown",
            // ScholarshipProvider-side queries always operate on platform scholarships; the
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
