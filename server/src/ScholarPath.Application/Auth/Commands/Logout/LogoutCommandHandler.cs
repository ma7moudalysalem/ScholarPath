using MediatR;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(ITokenService tokenService)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            await tokenService.RevokeRefreshTokenAsync(request.RefreshToken, "User logout", ct);
    }
}
