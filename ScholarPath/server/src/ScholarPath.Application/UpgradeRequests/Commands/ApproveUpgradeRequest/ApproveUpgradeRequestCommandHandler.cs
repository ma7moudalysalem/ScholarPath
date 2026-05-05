using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.UpgradeRequests.Commands.ApproveUpgradeRequest;

public class ApproveUpgradeRequestCommandHandler : IRequestHandler<ApproveUpgradeRequestCommand, ApproveUpgradeResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public ApproveUpgradeRequestCommandHandler(
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

    public async Task<ApproveUpgradeResponse> Handle(ApproveUpgradeRequestCommand request, CancellationToken cancellationToken)
    {
        var upgradeRequest = await _dbContext.UpgradeRequests
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (upgradeRequest is null)
        {
            throw new KeyNotFoundException("errors.admin.upgradeRequestNotFound");
        }

        if (upgradeRequest.Status != UpgradeRequestStatus.Pending)
        {
            throw new InvalidOperationException("errors.admin.onlyPendingCanBeApproved");
        }

        var adminUserIdStr = _currentUserService.UserId?.ToString();
        var adminUser = await _userManager.FindByIdAsync(adminUserIdStr ?? "00000000-0000-0000-0000-000000000000");

        upgradeRequest.Status = UpgradeRequestStatus.Approved;
        upgradeRequest.ReviewedAt = DateTime.UtcNow;
        upgradeRequest.ReviewedBy = adminUser?.Email;
        upgradeRequest.ReviewedById = adminUser?.Id;
        upgradeRequest.AdminNotes = request.ReviewNotes?.Trim() ?? "Approved by admin.";

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

        // Fire-and-forget email notification
        _ = Task.Run(async () =>
        {
            try
            {
                var user = upgradeRequest.User;
                await _emailService.SendUpgradeApprovedEmailAsync(
                    user.Email!,
                    $"{user.FirstName} {user.LastName}",
                    upgradeRequest.RequestedRole);
            }
            catch (Exception)
            {
                // Email failure should not affect the response
            }
        });

        return new ApproveUpgradeResponse(
            upgradeRequest.Id,
            upgradeRequest.Status.ToString(),
            upgradeRequest.ReviewedAt,
            upgradeRequest.ReviewedById
        );
    }
}
