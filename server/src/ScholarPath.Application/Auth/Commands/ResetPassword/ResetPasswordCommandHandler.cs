using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeService clock)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var resetToken = await db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > now, ct)
            ?? throw new ConflictException("Invalid or expired password-reset token.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == resetToken.UserId, ct)
            ?? throw new ConflictException("Invalid or expired password-reset token.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        resetToken.UsedAt = now;

        await db.SaveChangesAsync(ct);

        // A password reset locks every other active session out.
        await tokenService.RevokeAllForUserAsync(user.Id, "Password reset", ct);
    }
}
