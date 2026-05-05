using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.UpgradeRequests.Commands.RequestMoreInfoUpgradeRequest;

public class RequestMoreInfoUpgradeRequestCommandHandler : IRequestHandler<RequestMoreInfoUpgradeRequestCommand, RequestMoreInfoUpgradeResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public RequestMoreInfoUpgradeRequestCommandHandler(
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

    public async Task<RequestMoreInfoUpgradeResponse> Handle(RequestMoreInfoUpgradeRequestCommand request, CancellationToken cancellationToken)
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
            throw new InvalidOperationException("errors.admin.onlyPendingCanBeUpdated");
        }

        var adminUserIdStr = _currentUserService.UserId?.ToString();
        var adminUser = await _userManager.FindByIdAsync(adminUserIdStr ?? "00000000-0000-0000-0000-000000000000");

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

        return new RequestMoreInfoUpgradeResponse(
            upgradeRequest.Id,
            upgradeRequest.Status.ToString(),
            upgradeRequest.AdminNotes,
            upgradeRequest.ReviewedAt,
            upgradeRequest.ReviewedById
        );
    }
}
