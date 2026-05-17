using FluentValidation;
using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Auth.Commands.VerifyEmail;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Confirms a user's email address with the Identity confirmation token from
/// the verification link (FR-215).
/// </summary>
[Auditable(AuditAction.Update, "User", TargetIdProperty = nameof(UserId), SkipOnNull = false)]
public sealed record VerifyEmailCommand(Guid UserId, string Token) : IRequest;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class VerifyEmailCommandHandler(IEmailVerificationService emailVerification)
    : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var confirmed = await emailVerification
            .ConfirmEmailAsync(request.UserId, request.Token, ct)
            .ConfigureAwait(false);

        if (!confirmed)
            throw new ConflictException("Invalid or expired email-verification link.");
    }
}
