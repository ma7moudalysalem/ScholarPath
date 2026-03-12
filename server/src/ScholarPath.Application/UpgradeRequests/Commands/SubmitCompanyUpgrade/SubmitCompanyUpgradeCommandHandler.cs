using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgrade;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.UpgradeRequests.Commands.SubmitCompanyUpgrade;

public class SubmitCompanyUpgradeCommandHandler : IRequestHandler<SubmitCompanyUpgradeCommand, SubmitUpgradeResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubmitCompanyUpgradeCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<SubmitUpgradeResponse> Handle(SubmitCompanyUpgradeCommand request, CancellationToken cancellationToken)
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
}
