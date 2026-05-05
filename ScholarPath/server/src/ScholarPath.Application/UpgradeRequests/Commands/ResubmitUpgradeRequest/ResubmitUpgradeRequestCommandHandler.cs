using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgrade;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.UpgradeRequests.Commands.ResubmitUpgradeRequest;

public class ResubmitUpgradeRequestCommandHandler : IRequestHandler<ResubmitUpgradeRequestCommand, SubmitUpgradeResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResubmitUpgradeRequestCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<SubmitUpgradeResponse> Handle(ResubmitUpgradeRequestCommand request, CancellationToken cancellationToken)
    {
        var userIdStr = _currentUserService.UserId?.ToString();
        var user = await _userManager.FindByIdAsync(userIdStr ?? "00000000-0000-0000-0000-000000000000");

        if (user == null)
        {
            throw new UnauthorizedAccessException("errors.auth.userNotFound");
        }

        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(r => r.EducationEntries)
            .Include(r => r.ExpertiseTagsList)
            .Include(r => r.Links)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.UserId == user.Id, cancellationToken);

        if (upgradeRequest == null)
        {
            throw new KeyNotFoundException("errors.admin.upgradeRequestNotFound");
        }

        if (upgradeRequest.Status != UpgradeRequestStatus.NeedsMoreInfo)
        {
            throw new InvalidOperationException("errors.upgradeRequest.canOnlyResubmitNeedsMoreInfo");
        }

        // Update consultant fields if provided
        if (request.ExperienceSummary is not null) upgradeRequest.ExperienceSummary = request.ExperienceSummary;
        if (request.Languages is not null) upgradeRequest.Languages = string.Join(",", request.Languages);

        if (request.Education is not null)
        {
            _dbContext.EducationEntries.RemoveRange(upgradeRequest.EducationEntries);
            upgradeRequest.EducationEntries = request.Education.Select(e => new EducationEntry
            {
                InstitutionName = e.InstitutionName,
                DegreeName = e.DegreeName,
                FieldOfStudy = e.FieldOfStudy,
                StartYear = e.StartYear,
                EndYear = e.EndYear,
                IsCurrentlyStudying = e.IsCurrentlyStudying
            }).ToList();
        }

        if (request.ExpertiseTags is not null)
        {
            upgradeRequest.ExpertiseTagsList = await ResolveExpertiseTagsAsync(request.ExpertiseTags, cancellationToken);
        }

        if (request.Links is not null)
        {
            var linksToRemove = await _dbContext.UpgradeRequestLinks.Where(l => l.UpgradeRequestId == upgradeRequest.Id).ToListAsync(cancellationToken);
            _dbContext.UpgradeRequestLinks.RemoveRange(linksToRemove);

            upgradeRequest.Links = request.Links.Select(l => new UpgradeRequestLink
            {
                Url = l.Url,
                Label = Enum.Parse<LinkLabel>(l.Label, ignoreCase: true)
            }).ToList();
        }

        // Update company fields if provided
        if (request.CompanyName is not null) upgradeRequest.CompanyName = request.CompanyName;
        if (request.Country is not null) upgradeRequest.CompanyCountry = request.Country;
        if (request.Website is not null) upgradeRequest.CompanyWebsite = request.Website;
        if (request.ContactPersonName is not null) upgradeRequest.ContactPersonName = request.ContactPersonName;
        if (request.ContactEmail is not null) upgradeRequest.ContactEmail = request.ContactEmail;
        if (request.ContactPhone is not null) upgradeRequest.ContactPhone = request.ContactPhone;
        if (request.CompanyRegistrationNumber is not null) upgradeRequest.CompanyRegistrationNumber = request.CompanyRegistrationNumber;

        upgradeRequest.Status = UpgradeRequestStatus.Pending;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitUpgradeResponse(upgradeRequest.Id, upgradeRequest.Status.ToString());
    }

    private async Task<List<ExpertiseTag>> ResolveExpertiseTagsAsync(
        List<string> tagNames, CancellationToken cancellationToken)
    {
        var result = new List<ExpertiseTag>();
        foreach (var name in tagNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = await _dbContext.ExpertiseTags
                .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

            if (existing != null)
            {
                result.Add(existing);
            }
            else
            {
                var newTag = new ExpertiseTag { Name = name };
                _dbContext.ExpertiseTags.Add(newTag);
                result.Add(newTag);
            }
        }
        return result;
    }
}
