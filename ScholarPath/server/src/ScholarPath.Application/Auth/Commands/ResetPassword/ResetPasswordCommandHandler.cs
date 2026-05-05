using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _dbContext;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("errors.auth.invalidResetToken");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("errors.auth.invalidResetToken");
        }

        // FR-025: Invalidate all active refresh tokens after password reset
        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedReason = "Password reset";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

