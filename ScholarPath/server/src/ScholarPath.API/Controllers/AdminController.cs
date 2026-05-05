using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.UpgradeRequests.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Application.UpgradeRequests.Commands.RejectUpgradeRequest;
using ScholarPath.Application.UpgradeRequests.Commands.ApproveUpgradeRequest;
using ScholarPath.Application.UpgradeRequests.Commands.RequestMoreInfoUpgradeRequest;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : BaseController
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public AdminController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailService = emailService;
    }

    [HttpGet("upgrade-requests")]
    public async Task<IActionResult> GetUpgradeRequests(
        [FromQuery] UpgradeRequestStatus? status = null,
        [FromQuery] UserRole? type = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UpgradeRequests
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (type.HasValue)
            query = query.Where(r => r.RequestedRole == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.User.FirstName.ToLower().Contains(term) ||
                r.User.LastName.ToLower().Contains(term) ||
                (r.User.Email != null && r.User.Email.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                UserEmail = r.User.Email,
                UserName = r.User.FirstName + " " + r.User.LastName,
                r.RequestedRole,
                r.Status,
                r.AdminNotes,
                r.RejectionReasons,
                r.ReviewedBy,
                r.ReviewedById,
                r.ReviewedAt,
                r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { items = requests, totalCount = total, page, pageSize, totalPages });
    }

    [HttpGet("upgrade-requests/{id:guid}")]
    public async Task<IActionResult> GetUpgradeRequestDetail(Guid id, CancellationToken cancellationToken)
    {
        var r = await _dbContext.UpgradeRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.EducationEntries)
            .Include(x => x.ExpertiseTagsList)
            .Include(x => x.Links)
            .Include(x => x.Files)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (r is null) return NotFoundResult("errors.admin.upgradeRequestNotFound");

        var detail = new UpgradeRequestDetailDto(
            r.Id, r.UserId, r.User.Email!, $"{r.User.FirstName} {r.User.LastName}",
            r.RequestedRole, r.Status, r.AdminNotes, r.RejectionReason, r.RejectionReasons,
            r.ReviewedBy, r.ReviewedAt, r.CreatedAt,
            r.ExperienceSummary,
            r.EducationEntries.Select(e => new EducationEntryDto(
                e.InstitutionName, e.DegreeName, e.FieldOfStudy,
                e.StartYear, e.EndYear, e.IsCurrentlyStudying)).ToList(),
            r.ExpertiseTagsList.Select(t => t.Name).ToList(),
            r.Languages?.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList(),
            r.Links.Select(l => new UpgradeRequestLinkDto(l.Url, l.Label.ToString())).ToList(),
            r.Files.Select(f => new UpgradeRequestFileDto(f.Id, f.FileName, f.ContentType, f.FileSize, f.UploadedAt)).ToList(),
            r.CompanyName, r.CompanyCountry, r.CompanyWebsite,
            r.ContactPersonName, r.ContactEmail, r.ContactPhone, r.CompanyRegistrationNumber);

        return Ok(detail);
    }

    [HttpPut("upgrade-requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveUpgradeRequest(Guid id, [FromBody] UpgradeReviewRequest? request = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ApproveUpgradeRequestCommand(id, request?.ReviewNotes);
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpPut("upgrade-requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectUpgradeRequest(Guid id, [FromBody] UpgradeRejectRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new RejectUpgradeRequestCommand(id, request.ReviewNotes, request.RejectionReasons);
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }

    [HttpPut("upgrade-requests/{id:guid}/request-info")]
    public async Task<IActionResult> RequestMoreInfo(Guid id, [FromBody] UpgradeReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewNotes))
            return BadRequestResult("errors.admin.reviewNotesRequired");

        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (upgradeRequest is null)
            return NotFoundResult("errors.admin.upgradeRequestNotFound");

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
            return BadRequestResult("errors.admin.onlyPendingCanBeUpdated");

        var adminUser = await _userManager.GetUserAsync(User);

        upgradeRequest.Status = UpgradeRequestStatus.NeedsMoreInfo;
        upgradeRequest.AdminNotes = request.ReviewNotes.Trim();
        upgradeRequest.ReviewedAt = DateTime.UtcNow;
        upgradeRequest.ReviewedBy = adminUser?.Email;
        upgradeRequest.ReviewedById = adminUser?.Id;

        upgradeRequest.User.Role = UserRole.Unassigned;
        upgradeRequest.User.AccountStatus = AccountStatus.Pending;

        _dbContext.Notifications.Add(new Notification
        {
            UserId = upgradeRequest.UserId,
            Type = NotificationType.UpgradeStatus,
            Title = "More information required",
            Message = "Admin requested more details for your upgrade request.",
            RelatedEntityId = upgradeRequest.Id,
            RelatedEntityType = nameof(UpgradeRequest)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Fire-and-forget email notification
        _ = Task.Run(async () =>
        {
            try
            {
                var user = upgradeRequest.User;
                await _emailService.SendNeedsMoreInfoEmailAsync(
                    user.Email!,
                    $"{user.FirstName} {user.LastName}",
                    upgradeRequest.AdminNotes);
            }
            catch (Exception)
            {
                // Email failure should not affect the response
            }
        });

        return Ok(new
        {
            upgradeRequest.Id,
            upgradeRequest.Status,
            upgradeRequest.AdminNotes,
            upgradeRequest.ReviewedAt,
            upgradeRequest.ReviewedById
        });
    }

    public record UpgradeReviewRequest(string? ReviewNotes);
    public record UpgradeRejectRequest(string? ReviewNotes, List<string>? RejectionReasons);
}
