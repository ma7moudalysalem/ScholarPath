using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.UpgradeRequests.Commands.RejectUpgradeRequest;

public class RejectUpgradeRequestCommandHandler : IRequestHandler<RejectUpgradeRequestCommand, RejectUpgradeResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public RejectUpgradeRequestCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<RejectUpgradeResponse> Handle(RejectUpgradeRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewNotes))
        {
            throw new ArgumentException("errors.admin.reviewNotesRequired");
        }

        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (upgradeRequest is null)
        {
            throw new KeyNotFoundException("errors.admin.upgradeRequestNotFound");
        }

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
        {
            throw new InvalidOperationException("errors.admin.onlyPendingCanBeRejected");
        }

        var adminUserIdStr = _currentUserService.UserId?.ToString();
        var adminUser = await _userManager.FindByIdAsync(adminUserIdStr ?? "00000000-0000-0000-0000-000000000000");

        upgradeRequest.Status = UpgradeRequestStatus.Rejected;
        upgradeRequest.AdminNotes = request.ReviewNotes.Trim();
        upgradeRequest.RejectionReason = request.ReviewNotes.Trim();
        upgradeRequest.ReviewedAt = DateTime.UtcNow;
        upgradeRequest.ReviewedBy = adminUser?.Email;
        upgradeRequest.ReviewedById = adminUser?.Id;

        if (request.RejectionReasons is { Count: > 0 })
        {
            upgradeRequest.RejectionReasons = JsonSerializer.Serialize(request.RejectionReasons);
        }

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

        // Fire-and-forget email notification
        _ = Task.Run(async () =>
        {
            try
            {
                var user = upgradeRequest.User;
                await _emailService.SendUpgradeRejectedEmailAsync(
                    user.Email!,
                    $"{user.FirstName} {user.LastName}",
                    upgradeRequest.RejectionReasons);
            }
            catch (Exception)
            {
                // Email failure should not affect the response
            }
        });

        return new RejectUpgradeResponse(
            upgradeRequest.Id,
            upgradeRequest.Status.ToString(),
            upgradeRequest.AdminNotes,
            upgradeRequest.RejectionReasons,
            upgradeRequest.ReviewedAt,
            upgradeRequest.ReviewedById
        );
    }
}
