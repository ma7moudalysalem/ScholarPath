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
            .Include(u => u.Profile)
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
                u.ActiveRole,
                u.Profile != null ? u.Profile.OrganizationLegalName : null,
                u.Profile != null ? u.Profile.OrganizationWebsite : null,
                u.Profile != null ? u.Profile.OrganizationEmail : null,
                u.Profile != null ? u.Profile.OrganizationCountry : null,
                u.Profile != null ? u.Profile.CompanyType : null,
                u.Profile != null ? u.Profile.CompanyDescription : null,
                u.Profile != null ? u.Profile.OrganizationRegistrationNumber : null,
                u.Profile != null ? u.Profile.OrganizationTaxNumber : null,
                u.Profile != null ? u.Profile.ContactPersonFullName : null,
                u.Profile != null ? u.Profile.ContactPersonPosition : null,
                u.Profile != null ? u.Profile.ContactPhoneNumber : null,
                u.Profile != null ? u.Profile.Biography : null,
                u.Profile != null ? u.Profile.ProfessionalTitle : null,
                u.Profile != null ? u.Profile.HighestDegree : null,
                u.Profile != null ? u.Profile.FieldOfExpertise : null,
                u.Profile != null ? u.Profile.YearsOfExperience : null,
                u.Profile != null ? u.Profile.SessionFeeUsd : null,
                u.Profile != null ? u.Profile.SessionDurationMinutes : null,
                u.Profile != null ? u.Profile.ExpertiseTagsJson : null,
                u.Profile != null ? u.Profile.LanguagesJson : null,
                u.Profile != null ? u.Profile.Timezone : null,
                u.Profile != null ? u.Profile.LinkedInUrl : null,
                u.Profile != null ? u.Profile.PortfolioUrl : null,
                u.CountryOfResidence))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<OnboardingRequestRow>(rows, page, pageSize, total);
    }
}
