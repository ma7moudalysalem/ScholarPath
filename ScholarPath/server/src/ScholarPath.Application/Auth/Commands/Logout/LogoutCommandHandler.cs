using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;


namespace ScholarPath.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserAdministration _userAdministration;

    public LogoutCommandHandler(
        IApplicationDbContext dbContext,
        IUserAdministration userAdministration)
    {
        _dbContext = dbContext;
        _userAdministration = userAdministration;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash
                                     && !t.IsRevoked, cancellationToken);

        if (refreshToken is null)
            return Unit.Value;

        if (request.LogoutEverywhere)
        {
            await _userAdministration.RevokeAllSessionsAsync(
                refreshToken.UserId,
                "User logout (everywhere)",
                cancellationToken);
        }
        else
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            refreshToken.RevokedReason = "User logout";
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
