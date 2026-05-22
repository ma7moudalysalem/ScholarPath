using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Commands.ChangePassword;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Lets an authenticated user update their own password.
/// On success all existing refresh tokens for the account are revoked so
/// any other sessions must re-authenticate (PB-002 T-005).
/// </summary>
public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Unit>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ChangePasswordCommandValidator
    : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        // CR-PROF-05: match the registration / reset-password policy so all three
        // flows enforce the same strength — at least one upper, one lower, one
        // digit and one special character; 8–128 chars; not equal to the old one.
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("New password cannot exceed 128 characters.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("New password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("New password must contain at least one special character.")
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must differ from the current password.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ChangePasswordCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher,
    IDateTimeService clock)
    : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(
        ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        // SSO-only accounts have no password — reject gracefully.
        if (string.IsNullOrEmpty(user.PasswordHash))
            throw new ConflictException(
                "Your account uses social sign-in and does not have a password.");

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            throw new ConflictException("The current password is incorrect.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        // Revoke all refresh tokens so other sessions must re-authenticate.
        var now = clock.UtcNow;
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.IsRevoked   = true;
            token.RevokedAt   = now;
            token.RevokedReason = "password_changed";
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Unit.Value;
    }
}
