using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgrade;

public class SubmitConsultantUpgradeCommandHandler : IRequestHandler<SubmitConsultantUpgradeCommand, SubmitUpgradeResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubmitConsultantUpgradeCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<SubmitUpgradeResponse> Handle(SubmitConsultantUpgradeCommand request, CancellationToken cancellationToken)
    {
        var userIdStr = _currentUserService.UserId?.ToString();
        var user = await _userManager.FindByIdAsync(userIdStr ?? "00000000-0000-0000-0000-000000000000");

        if (user == null)
        {
            throw new UnauthorizedAccessException("errors.auth.userNotFound");
        }

        var hasPending = await _dbContext.UpgradeRequests
            .AnyAsync(r => r.UserId == user.Id && r.Status == UpgradeRequestStatus.Pending, cancellationToken);

        if (hasPending)
        {
            throw new InvalidOperationException("errors.auth.pendingUpgradeExists");
        }

        var expertiseTags = await ResolveExpertiseTagsAsync(request.ExpertiseTags, cancellationToken);

        var upgradeRequest = new UpgradeRequest
        {
            UserId = user.Id,
            RequestedRole = UserRole.Consultant,
            Status = UpgradeRequestStatus.Pending,
            ExperienceSummary = request.ExperienceSummary,
            Languages = string.Join(",", request.Languages),
            EducationEntries = request.Education.Select(e => new EducationEntry
            {
                InstitutionName = e.InstitutionName,
                DegreeName = e.DegreeName,
                FieldOfStudy = e.FieldOfStudy,
                StartYear = e.StartYear,
                EndYear = e.EndYear,
                IsCurrentlyStudying = e.IsCurrentlyStudying
            }).ToList(),
            ExpertiseTagsList = expertiseTags,
            Links = request.Links?.Select(l => new UpgradeRequestLink
            {
                Url = l.Url,
                Label = Enum.Parse<LinkLabel>(l.Label, ignoreCase: true)
            }).ToList() ?? new List<UpgradeRequestLink>()
        };

        user.AccountStatus = AccountStatus.Pending;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _dbContext.UpgradeRequests.Add(upgradeRequest);
            await _userManager.UpdateAsync(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

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
