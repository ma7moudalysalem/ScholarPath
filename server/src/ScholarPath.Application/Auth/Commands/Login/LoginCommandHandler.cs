using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUserAdministration userAdministration,
    IDateTimeService clock)
    : IRequestHandler<LoginCommand, AuthTokensDto>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    public async Task<AuthTokensDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var now = clock.UtcNow;

        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (user is null)
        {
            db.LoginAttempts.Add(BuildAttempt(normalizedEmail, null, false, "User not found", request, now));
            await db.SaveChangesAsync(ct);
            throw new ConflictException("Invalid email or password.");
        }

        if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
        {
            db.LoginAttempts.Add(BuildAttempt(normalizedEmail, user.Id, false, "Account locked", request, now));
            await db.SaveChangesAsync(ct);
            throw new ConflictException("Account is temporarily locked. Please try again later.");
        }

        var passwordValid = !string.IsNullOrEmpty(user.PasswordHash)
            && passwordHasher.Verify(user.PasswordHash, request.Password);

        if (!passwordValid)
        {
            db.LoginAttempts.Add(BuildAttempt(normalizedEmail, user.Id, false, "Wrong password", request, now));

            // Count *consecutive* failures: inside the rolling window AND since the last successful login.
            var windowStart = now - LockoutWindow;
            if (user.LastLoginAt is { } lastLogin && lastLogin > windowStart)
                windowStart = lastLogin;

            var priorFailures = await db.LoginAttempts
                .CountAsync(a => a.Email == normalizedEmail && !a.Succeeded && a.OccurredAt >= windowStart, ct);

            if (priorFailures + 1 >= MaxFailedAttempts)
            {
                user.LockoutEnd = now + LockoutDuration;
                user.AccessFailedCount = priorFailures + 1;
            }

            await db.SaveChangesAsync(ct);
            throw new ConflictException("Invalid email or password.");
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = now;
        db.LoginAttempts.Add(BuildAttempt(normalizedEmail, user.Id, true, null, request, now));
        await db.SaveChangesAsync(ct);

        var roles = await userAdministration.GetRolesAsync(user.Id, ct);
        var tokens = tokenService.IssueTokens(user, roles, user.ActiveRole, request.RememberMe);
        return AuthDtoFactory.Build(tokens, user, roles);
    }

    private static LoginAttempt BuildAttempt(
        string normalizedEmail, Guid? userId, bool succeeded, string? reason, LoginCommand request, DateTimeOffset now) =>
        new()
        {
            Email = normalizedEmail,
            UserId = userId,
            Succeeded = succeeded,
            FailureReason = reason,
            OccurredAt = now,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
        };
}
