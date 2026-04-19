using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;

    public LogoutCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken
                                     && !t.IsRevoked, cancellationToken);

        if (refreshToken is null)
        {
            return Unit.Value;
        }

        if (request.LogoutEverywhere)
        {
            var allTokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == refreshToken.UserId
                           && !t.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in allTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "User logout (everywhere)";
            }
        }
        else
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedReason = "User logout";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}