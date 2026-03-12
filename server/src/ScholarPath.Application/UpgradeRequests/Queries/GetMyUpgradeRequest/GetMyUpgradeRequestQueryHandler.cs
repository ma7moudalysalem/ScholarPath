using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.UpgradeRequests.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.UpgradeRequests.Queries.GetMyUpgradeRequest;

public class GetMyUpgradeRequestQueryHandler : IRequestHandler<GetMyUpgradeRequestQuery, UpgradeRequestDetailDto?>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetMyUpgradeRequestQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<UpgradeRequestDetailDto?> Handle(GetMyUpgradeRequestQuery request, CancellationToken cancellationToken)
    {
        var userIdStr = _currentUserService.UserId?.ToString();
        var user = await _userManager.FindByIdAsync(userIdStr ?? "00000000-0000-0000-0000-000000000000");

        if (user == null)
        {
            throw new UnauthorizedAccessException("errors.auth.userNotFound");
        }

        var latest = await _dbContext.UpgradeRequests
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new UpgradeRequestDetailDto(
                r.Id, r.UserId, r.User.Email!, $"{r.User.FirstName} {r.User.LastName}",
                r.RequestedRole, r.Status, r.AdminNotes, r.RejectionReason, r.RejectionReasons,
                r.ReviewedBy, r.ReviewedAt, r.CreatedAt,
                r.ExperienceSummary,
                r.EducationEntries.Select(e => new EducationEntryDto(
                    e.InstitutionName, e.DegreeName, e.FieldOfStudy,
                    e.StartYear, e.EndYear, e.IsCurrentlyStudying)).ToList(),
                r.ExpertiseTagsList.Select(t => t.Name).ToList(),
                r.Languages != null ? r.Languages.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList() : null,
                r.Links.Select(l => new UpgradeRequestLinkDto(l.Url, l.Label.ToString())).ToList(),
                r.Files.Select(f => new UpgradeRequestFileDto(f.Id, f.FileName, f.ContentType, f.FileSize, f.UploadedAt)).ToList(),
                r.CompanyName, r.CompanyCountry, r.CompanyWebsite,
                r.ContactPersonName, r.ContactEmail, r.ContactPhone, r.CompanyRegistrationNumber))
            .FirstOrDefaultAsync(cancellationToken);

        return latest;
    }
}
