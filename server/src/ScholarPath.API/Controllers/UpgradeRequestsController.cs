using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/upgrade-requests")]
[Authorize]
public class UpgradeRequestsController : BaseController
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UpgradeRequestsController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    // POST /api/v1/upgrade-requests/consultant
    [HttpPost("consultant")]
    public async Task<IActionResult> SubmitConsultantUpgrade(
        [FromBody] SubmitConsultantUpgradeRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        // Duplicate prevention: block if there is already a Pending request
        var hasPending = await _dbContext.UpgradeRequests
            .AnyAsync(r => r.UserId == user.Id && r.Status == UpgradeRequestStatus.Pending, cancellationToken);

        if (hasPending)
            return Conflict("You already have a pending upgrade request.");

        // block if already active consultant/company
        if (user.Role is UserRole.Consultant or UserRole.Company && user.AccountStatus == AccountStatus.Active)
            return BadRequest("Your account is already upgraded.");

        var upgrade = new UpgradeRequest
        {
            UserId = user.Id,
            RequestedRole = UserRole.Consultant,
            Status = UpgradeRequestStatus.Pending,

            // Consultant fields
            ExperienceSummary = dto.ExperienceSummary?.Trim(),
            ExpertiseTags = dto.ExpertiseTags is { Count: > 0 } ? string.Join(",", dto.ExpertiseTags.Select(x => x.Trim())) : null,
            Languages = dto.Languages is { Count: > 0 } ? string.Join(",", dto.Languages.Select(x => x.Trim())) : null,
            LinkedInUrl = dto.LinkedInUrl?.Trim(),
            PortfolioUrl = dto.PortfolioUrl?.Trim(),
            ProofDocumentUrl = dto.ProofDocumentUrl?.Trim()
        };

        // Business flow: consultant/company are pending until admin review
        user.AccountStatus = AccountStatus.Pending;
        user.Role = UserRole.Unassigned;

        _dbContext.UpgradeRequests.Add(upgrade);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            upgrade.Id,
            upgrade.RequestedRole,
            upgrade.Status
        });
    }

    // POST /api/v1/upgrade-requests/company
    [HttpPost("company")]
    public async Task<IActionResult> SubmitCompanyUpgrade(
        [FromBody] SubmitCompanyUpgradeRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        // Duplicate prevention: block if there is already a Pending request
        var hasPending = await _dbContext.UpgradeRequests
            .AnyAsync(r => r.UserId == user.Id && r.Status == UpgradeRequestStatus.Pending, cancellationToken);

        if (hasPending)
            return Conflict("You already have a pending upgrade request.");

        if (user.Role is UserRole.Consultant or UserRole.Company && user.AccountStatus == AccountStatus.Active)
            return BadRequest("Your account is already upgraded.");

        var upgrade = new UpgradeRequest
        {
            UserId = user.Id,
            RequestedRole = UserRole.Company,
            Status = UpgradeRequestStatus.Pending,

            // Company fields
            CompanyName = dto.CompanyName?.Trim(),
            CompanyCountry = dto.Country?.Trim(),
            CompanyWebsite = dto.Website?.Trim(),
            ContactPersonName = dto.ContactPersonName?.Trim(),
            ContactEmail = dto.ContactEmail?.Trim(),
            ContactPhone = dto.ContactPhone?.Trim(),
            CompanyRegistrationNumber = dto.CompanyRegistrationNumber?.Trim(),
            ProofDocumentUrl = dto.ProofDocumentUrl?.Trim()
        };

        user.AccountStatus = AccountStatus.Pending;
        user.Role = UserRole.Unassigned;

        _dbContext.UpgradeRequests.Add(upgrade);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            upgrade.Id,
            upgrade.RequestedRole,
            upgrade.Status
        });
    }

    // GET /api/v1/upgrade-requests/my-status
    [HttpGet("my-status")]
    public async Task<IActionResult> GetMyLatestStatus(CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var latest = await _dbContext.UpgradeRequests
            .AsNoTracking()
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
            return NotFound("No upgrade request found for this user.");

        return Ok(new
        {
            latest.Id,
            latest.RequestedRole,
            latest.Status,
            latest.AdminNotes,
            latest.RejectionReason,
            latest.RejectionReasons,
            latest.ReviewedBy,
            latest.ReviewedAt,
            latest.CreatedAt
        });
    }

    // PUT /api/v1/upgrade-requests/{id}/resubmit
    [HttpPut("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(
        Guid id,
        [FromBody] ResubmitUpgradeRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var req = await _dbContext.UpgradeRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id, cancellationToken);

        if (req is null)
            return NotFound("Upgrade request not found.");

        // Resubmit only allowed after "Needs More Info"
        if (req.Status != UpgradeRequestStatus.NeedsMoreInfo)
            return BadRequest("Resubmit is only allowed when status is NeedsMoreInfo.");

        // Update fields depending on requested role
        if (req.RequestedRole == UserRole.Consultant)
        {
            req.ExperienceSummary = dto.ExperienceSummary?.Trim();
            req.ExpertiseTags = dto.ExpertiseTags is { Count: > 0 } ? string.Join(",", dto.ExpertiseTags.Select(x => x.Trim())) : null;
            req.Languages = dto.Languages is { Count: > 0 } ? string.Join(",", dto.Languages.Select(x => x.Trim())) : null;
            req.LinkedInUrl = dto.LinkedInUrl?.Trim();
            req.PortfolioUrl = dto.PortfolioUrl?.Trim();
            req.ProofDocumentUrl = dto.ProofDocumentUrl?.Trim();
        }
        else if (req.RequestedRole == UserRole.Company)
        {
            req.CompanyName = dto.CompanyName?.Trim();
            req.CompanyCountry = dto.Country?.Trim();
            req.CompanyWebsite = dto.Website?.Trim();
            req.ContactPersonName = dto.ContactPersonName?.Trim();
            req.ContactEmail = dto.ContactEmail?.Trim();
            req.ContactPhone = dto.ContactPhone?.Trim();
            req.CompanyRegistrationNumber = dto.CompanyRegistrationNumber?.Trim();
            req.ProofDocumentUrl = dto.ProofDocumentUrl?.Trim();
        }

        // Reset review-related fields and re-queue for admin review
        req.Status = UpgradeRequestStatus.Pending;
        req.ReviewedAt = null;
        req.ReviewedBy = null;
        req.RejectionReason = null;
        req.RejectionReasons = null;


        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            req.Id,
            req.RequestedRole,
            req.Status
        });
    }

    //DTOs 

    public record SubmitConsultantUpgradeRequestDto(
        string? ExperienceSummary,
        List<string>? ExpertiseTags,
        List<string>? Languages,
        string? LinkedInUrl,
        string? PortfolioUrl,
        string? ProofDocumentUrl
    );

    public record SubmitCompanyUpgradeRequestDto(
        string CompanyName,
        string Country,
        string? Website,
        string ContactPersonName,
        string ContactEmail,
        string? ContactPhone,
        string CompanyRegistrationNumber,
        string? ProofDocumentUrl
    );

    public record ResubmitUpgradeRequestDto(
        // Consultant fields
        string? ExperienceSummary,
        List<string>? ExpertiseTags,
        List<string>? Languages,
        string? LinkedInUrl,
        string? PortfolioUrl,

        // Company fields
        string? CompanyName,
        string? Country,
        string? Website,
        string? ContactPersonName,
        string? ContactEmail,
        string? ContactPhone,
        string? CompanyRegistrationNumber,

        // Shared
        string? ProofDocumentUrl
    );
}
