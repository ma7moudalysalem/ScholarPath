using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : BaseController
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly HashSet<string> ValidRejectionCodes =
    [
        "missing_crn",
        "proof_not_clear",
        "suspicious_request",
        "incomplete_profile",
        "invalid_documents",
        "duplicate_request",
        "other"
    ];

    public AdminController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet("upgrade-requests")]
    public async Task<IActionResult> GetUpgradeRequests(
        [FromQuery] UpgradeRequestStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        IQueryable<UpgradeRequest> query = _dbContext.UpgradeRequests.AsNoTracking();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var requests = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                UserEmail = r.User.Email,
                UserName = $"{r.User.FirstName} {r.User.LastName}",
                r.RequestedRole,
                r.Status,
                r.AdminNotes,
                r.RejectionReasons,
                r.CreatedAt,
                r.ReviewedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpPut("upgrade-requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveUpgradeRequest(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (upgradeRequest is null)
            return NotFoundResult("errors.admin.upgradeRequestNotFound");

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
            return BadRequestResult("errors.admin.onlyPendingCanBeApproved");

        var adminUser = await _userManager.GetUserAsync(User);

        upgradeRequest.Status = UpgradeRequestStatus.Approved;
        upgradeRequest.ReviewedAt = DateTime.UtcNow;
        upgradeRequest.ReviewedBy = adminUser?.Email;

        upgradeRequest.User.Role = upgradeRequest.RequestedRole;
        upgradeRequest.User.AccountStatus = AccountStatus.Active;
        upgradeRequest.User.IsOnboardingComplete = true;

        _dbContext.Notifications.Add(new Notification
        {
            UserId = upgradeRequest.UserId,
            Type = NotificationType.UpgradeStatus,
            Title = "Upgrade approved",
            Message = "Your upgrade request has been approved. Welcome to your new role!",
            RelatedEntityId = upgradeRequest.Id,
            RelatedEntityType = nameof(UpgradeRequest)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            upgradeRequest.Id,
            upgradeRequest.Status,
            upgradeRequest.ReviewedAt
        });
    }

    [HttpPut("upgrade-requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectUpgradeRequest(
        Guid id,
        [FromBody] RejectUpgradeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Reasons is null || request.Reasons.Count == 0)
            return BadRequestResult("errors.admin.rejectionReasonsRequired");

        var invalidCodes = request.Reasons
            .Where(r => !ValidRejectionCodes.Contains(r.Code))
            .Select(r => r.Code)
            .Distinct()
            .ToList();

        if (invalidCodes.Count > 0)
            return BadRequestResult($"errors.admin.invalidRejectionCodes: {string.Join(", ", invalidCodes)}");

        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (upgradeRequest is null)
            return NotFoundResult("errors.admin.upgradeRequestNotFound");

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
            return BadRequestResult("errors.admin.onlyPendingCanBeRejected");

        var adminUser = await _userManager.GetUserAsync(User);

        var reasonsJson = JsonSerializer.Serialize(request.Reasons);

        upgradeRequest.Status = UpgradeRequestStatus.Rejected;
        upgradeRequest.RejectionReasons = reasonsJson;
        upgradeRequest.RejectionReason = string.Join(", ", request.Reasons.Select(r => r.Code));
        upgradeRequest.AdminNotes = request.ReviewNotes?.Trim();
        upgradeRequest.ReviewedAt = DateTime.UtcNow;
        upgradeRequest.ReviewedBy = adminUser?.Email;

        upgradeRequest.User.Role = UserRole.Unassigned;
        upgradeRequest.User.AccountStatus = AccountStatus.Rejected;

        _dbContext.Notifications.Add(new Notification
        {
            UserId = upgradeRequest.UserId,
            Type = NotificationType.UpgradeStatus,
            Title = "Upgrade rejected",
            Message = "Your upgrade request was rejected. Please review admin notes and submit again.",
            RelatedEntityId = upgradeRequest.Id,
            RelatedEntityType = nameof(UpgradeRequest)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            upgradeRequest.Id,
            upgradeRequest.Status,
            upgradeRequest.RejectionReasons,
            upgradeRequest.AdminNotes,
            upgradeRequest.ReviewedAt
        });
    }

    [HttpPut("upgrade-requests/{id:guid}/request-info")]
    public async Task<IActionResult> RequestMoreInfo(
        Guid id,
        [FromBody] UpgradeReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeReviewNotes(request, out var reviewNotes, out var validationError))
            return BadRequestResult(validationError!);

        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (upgradeRequest is null)
            return NotFoundResult("errors.admin.upgradeRequestNotFound");

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
            return BadRequestResult("errors.admin.onlyPendingCanBeUpdated");

        var adminUser = await _userManager.GetUserAsync(User);

        upgradeRequest.Status = UpgradeRequestStatus.NeedsMoreInfo;
        upgradeRequest.AdminNotes = reviewNotes;
        upgradeRequest.ReviewedAt = DateTime.UtcNow;
        upgradeRequest.ReviewedBy = adminUser?.Email;

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

        return Ok(new
        {
            upgradeRequest.Id,
            upgradeRequest.Status,
            upgradeRequest.AdminNotes,
            upgradeRequest.ReviewedAt
        });
    }

    private static bool TryNormalizeReviewNotes(
        UpgradeReviewRequest request,
        out string? reviewNotes,
        out string? validationError)
    {
        reviewNotes = request.ReviewNotes?.Trim();

        if (string.IsNullOrWhiteSpace(reviewNotes))
        {
            validationError = "errors.admin.reviewNotesRequired";
            return false;
        }

        validationError = null;
        return true;
    }

    public record UpgradeReviewRequest(string? ReviewNotes);

    public record RejectUpgradeRequestDto(
        List<RejectionReasonDto> Reasons,
        string? ReviewNotes
    );

    public record RejectionReasonDto(
        string Code,
        string? Note
    );
}
