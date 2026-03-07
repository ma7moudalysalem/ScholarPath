using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.UpgradeRequests.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/upgrade-requests")]
[Authorize]
public class UpgradeRequestsController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidator<ConsultantUpgradeRequest> _consultantValidator;
    private readonly IValidator<CompanyUpgradeRequest> _companyValidator;

    public UpgradeRequestsController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IValidator<ConsultantUpgradeRequest> consultantValidator,
        IValidator<CompanyUpgradeRequest> companyValidator)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _consultantValidator = consultantValidator;
        _companyValidator = companyValidator;
    }

    [HttpPost("consultant")]
    public async Task<IActionResult> SubmitConsultant(
        [FromBody] ConsultantUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _consultantValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequestResult(validationResult.Errors.Select(e => e.ErrorMessage));

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return UnauthorizedResult("errors.auth.userNotFound");

        var hasPending = await _dbContext.UpgradeRequests
            .AnyAsync(r => r.UserId == user.Id && r.Status == UpgradeRequestStatus.Pending, cancellationToken);
        if (hasPending) return ConflictResult("errors.auth.pendingUpgradeExists");

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
        _dbContext.UpgradeRequests.Add(upgradeRequest);
        await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new { upgradeRequest.Id, upgradeRequest.Status });
    }

    [HttpPost("company")]
    public async Task<IActionResult> SubmitCompany(
        [FromBody] CompanyUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _companyValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequestResult(validationResult.Errors.Select(e => e.ErrorMessage));

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return UnauthorizedResult("errors.auth.userNotFound");

        var hasPending = await _dbContext.UpgradeRequests
            .AnyAsync(r => r.UserId == user.Id && r.Status == UpgradeRequestStatus.Pending, cancellationToken);
        if (hasPending) return ConflictResult("errors.auth.pendingUpgradeExists");

        var upgradeRequest = new UpgradeRequest
        {
            UserId = user.Id,
            RequestedRole = UserRole.Company,
            Status = UpgradeRequestStatus.Pending,
            CompanyName = request.CompanyName,
            CompanyCountry = request.Country,
            CompanyWebsite = request.Website,
            ContactPersonName = request.ContactPersonName,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            CompanyRegistrationNumber = request.CompanyRegistrationNumber
        };

        user.AccountStatus = AccountStatus.Pending;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.UpgradeRequests.Add(upgradeRequest);
        await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new { upgradeRequest.Id, upgradeRequest.Status });
    }

    [HttpGet("my-status")]
    public async Task<IActionResult> GetMyStatus(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return UnauthorizedResult("errors.auth.userNotFound");

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

        if (latest is null) return Ok(new { });
        return Ok(latest);
    }

    [HttpPut("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(
        Guid id,
        [FromBody] ResubmitUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return UnauthorizedResult("errors.auth.userNotFound");

        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(r => r.EducationEntries)
            .Include(r => r.ExpertiseTagsList)
            .Include(r => r.Links)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id, cancellationToken);

        if (upgradeRequest is null) return NotFoundResult("errors.admin.upgradeRequestNotFound");

        if (upgradeRequest.Status != UpgradeRequestStatus.NeedsMoreInfo)
            return BadRequestResult("errors.upgradeRequest.canOnlyResubmitNeedsMoreInfo");

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
            _dbContext.UpgradeRequestLinks.RemoveRange(upgradeRequest.Links);
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

        return Ok(new { upgradeRequest.Id, upgradeRequest.Status });
    }

    private async Task<List<ExpertiseTag>> ResolveExpertiseTagsAsync(
        List<string> tagNames, CancellationToken cancellationToken)
    {
        var result = new List<ExpertiseTag>();
        foreach (var name in tagNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = await _dbContext.ExpertiseTags
                .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
            result.Add(existing ?? new ExpertiseTag { Name = name });
        }
        return result;
    }
}
