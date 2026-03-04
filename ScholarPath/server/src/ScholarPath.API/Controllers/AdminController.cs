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
        var query = _dbContext.UpgradeRequests
            .AsNoTracking()
            .OrderByDescending(upgradeRequest => upgradeRequest.CreatedAt);

        if (status.HasValue)
        {
            query = (IOrderedQueryable<UpgradeRequest>)query.Where(upgradeRequest => upgradeRequest.Status == status.Value);
        }

        var requests = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(upgradeRequest => new
            {
                upgradeRequest.Id,
                upgradeRequest.UserId,
                UserEmail = upgradeRequest.User.Email,
                UserName = $"{upgradeRequest.User.FirstName} {upgradeRequest.User.LastName}",
                upgradeRequest.RequestedRole,
                upgradeRequest.Status,
                upgradeRequest.AdminNotes,
                upgradeRequest.CreatedAt,
                upgradeRequest.ReviewedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpPut("upgrade-requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveUpgradeRequest(Guid id, [FromBody] UpgradeReviewRequest? request = null, CancellationToken cancellationToken = default)
    {
        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (upgradeRequest is null)
        {
            return NotFoundResult("errors.admin.upgradeRequestNotFound");
        }

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
        {
            return BadRequestResult("errors.admin.onlyPendingCanBeApproved");
        }

        var adminUser = await _userManager.GetUserAsync(User);

        upgradeRequest.Status = UpgradeRequestStatus.Approved;
        upgradeRequest.ReviewedAt = DateTime.UtcNow;
        upgradeRequest.ReviewedBy = adminUser?.Email;
        upgradeRequest.AdminNotes = request?.ReviewNotes?.Trim() ?? "Approved by admin.";

        upgradeRequest.User.Role = upgradeRequest.RequestedRole;
        upgradeRequest.User.AccountStatus = AccountStatus.Active;

        _dbContext.Notifications.Add(new Notification
        {
            UserId = upgradeRequest.UserId,
            Type = NotificationType.UpgradeStatus,
            Title = "Upgrade approved",
            Message = $"Your request for {upgradeRequest.RequestedRole} access has been approved.",
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
    public async Task<IActionResult> RejectUpgradeRequest(Guid id, [FromBody] UpgradeReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeReviewNotes(request, out var reviewNotes, out var validationError))
        {
            return BadRequestResult(validationError!);
        }

        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (upgradeRequest is null)
        {
            return NotFoundResult("errors.admin.upgradeRequestNotFound");
        }

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
        {
            return BadRequestResult("errors.admin.onlyPendingCanBeRejected");
        }

        var adminUser = await _userManager.GetUserAsync(User);

        upgradeRequest.Status = UpgradeRequestStatus.Rejected;
        upgradeRequest.AdminNotes = reviewNotes;
        upgradeRequest.RejectionReason = reviewNotes;
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
            upgradeRequest.AdminNotes,
            upgradeRequest.ReviewedAt
        });
    }

    [HttpPut("upgrade-requests/{id:guid}/request-info")]
    public async Task<IActionResult> RequestMoreInfo(Guid id, [FromBody] UpgradeReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeReviewNotes(request, out var reviewNotes, out var validationError))
        {
            return BadRequestResult(validationError!);
        }

        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (upgradeRequest is null)
        {
            return NotFoundResult("errors.admin.upgradeRequestNotFound");
        }

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
        {
            return BadRequestResult("errors.admin.onlyPendingCanBeUpdated");
        }

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

    private static bool TryNormalizeReviewNotes(UpgradeReviewRequest request, out string? reviewNotes, out string? validationError)
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
}
