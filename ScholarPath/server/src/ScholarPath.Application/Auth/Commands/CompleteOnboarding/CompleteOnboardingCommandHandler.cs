using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Application.Common.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand, UserDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CompleteOnboardingCommandHandler(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("errors.auth.userNotFound");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new UnauthorizedAccessException("errors.auth.userNotFound");
        }

        if (user.IsOnboardingComplete)
        {
            throw new InvalidOperationException("errors.auth.onboardingAlreadyComplete");
        }

        if (user.AccountStatus is AccountStatus.Suspended or AccountStatus.Rejected)
        {
            throw new UnauthorizedAccessException("errors.auth.accountNotEligibleForOnboarding");
        }

        if (request.SelectedRole == UserRole.Student)
        {
            user.Role = UserRole.Student;
            user.AccountStatus = AccountStatus.Active;
            user.IsOnboardingComplete = true;

            var studentUpdateResult = await _userManager.UpdateAsync(user);
            if (!studentUpdateResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", studentUpdateResult.Errors.Select(e => e.Description)));
            }

            return _mapper.Map<UserDto>(user);
        }

        var existingPendingRequest = await _dbContext.UpgradeRequests
            .AnyAsync(ur =>
                ur.UserId == user.Id &&
                ur.Status == UpgradeRequestStatus.Pending, cancellationToken);

        if (existingPendingRequest)
        {
            throw new InvalidOperationException("errors.auth.pendingUpgradeExists");
        }

        user.Role = UserRole.Unassigned;
        user.AccountStatus = AccountStatus.Pending;
        user.IsOnboardingComplete = true;

        var upgradeRequest = new UpgradeRequest
        {
            UserId = user.Id,
            RequestedRole = request.SelectedRole,
            Status = UpgradeRequestStatus.Pending,
            CompanyName = request.CompanyName,
            ExpertiseTags = request.ExpertiseArea,
            ExperienceSummary = request.Bio
        };

        var adminIds = await _userManager.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.UpgradeRequests.Add(upgradeRequest);

        foreach (var adminId in adminIds)
        {
            _dbContext.Notifications.Add(new Notification
            {
                UserId = adminId,
                Type = NotificationType.System,
                Title = "New upgrade request",
                Message = $"{user.FirstName} {user.LastName} requested {request.SelectedRole} access.",
                RelatedEntityId = upgradeRequest.Id,
                RelatedEntityType = nameof(UpgradeRequest)
            });
        }

        var upgradeUpdateResult = await _userManager.UpdateAsync(user);
        if (!upgradeUpdateResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(string.Join(", ", upgradeUpdateResult.Errors.Select(e => e.Description)));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }
}
