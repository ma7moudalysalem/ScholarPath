using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.ConfirmEmailChange;

public sealed class ConfirmEmailChangeCommandHandler(
    IApplicationDbContext db,
    IEmailChangeService emailChangeService,
    ITokenService tokenService,
    ICurrentUserService currentUser)
    : IRequestHandler<ConfirmEmailChangeCommand>
{
    public async Task Handle(ConfirmEmailChangeCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var newEmail = request.NewEmail.Trim();
        var normalizedNew = newEmail.ToUpperInvariant();

        // Re-check uniqueness at confirm time — another account may have
        // claimed the address between request and confirmation.
        var taken = await db.Users
            .AnyAsync(u => u.Id != userId && u.NormalizedEmail == normalizedNew, ct);
        if (taken)
            throw new ConflictException("That email address is already in use.");

        var ok = await emailChangeService
            .ConfirmEmailChangeAsync(userId, newEmail, request.Token, ct);
        if (!ok)
            throw new ConflictException("Invalid or expired email-change confirmation link.");

        // The email change rotates the security stamp — drop other sessions.
        await tokenService.RevokeAllForUserAsync(userId, "Email address changed", ct);
    }
}
