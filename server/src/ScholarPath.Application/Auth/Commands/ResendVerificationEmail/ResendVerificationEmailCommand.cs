using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.ResendVerificationEmail;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Re-sends the account email-verification link (FR-215). Always succeeds
/// silently — it never reveals whether the email is registered or already
/// verified.
/// </summary>
public sealed record ResendVerificationEmailCommand(string Email) : IRequest;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ResendVerificationEmailCommandValidator
    : AbstractValidator<ResendVerificationEmailCommand>
{
    public ResendVerificationEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ResendVerificationEmailCommandHandler(
    IApplicationDbContext db,
    IEmailVerificationService emailVerification,
    IEmailService emailService,
    IOptions<AppOptions> appOptions)
    : IRequestHandler<ResendVerificationEmailCommand>
{
    public async Task Handle(ResendVerificationEmailCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct)
            .ConfigureAwait(false);

        // Silent no-op when the email is unknown or already verified — no enumeration.
        if (user is null || user.EmailConfirmed)
            return;

        await EmailVerificationSender.SendAsync(
            user, emailVerification, emailService, appOptions.Value.ClientUrl, ct)
            .ConfigureAwait(false);
    }
}
