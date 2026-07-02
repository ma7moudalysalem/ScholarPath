using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeService clock,
    IEmailVerificationService emailVerification,
    IEmailService emailService,
    IOptions<AppOptions> appOptions)
    : IRequestHandler<RegisterCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        var emailTaken = await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);
        if (emailTaken)
            throw new ConflictException("An account with this email already exists.");

        var now = clock.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            UserName = email,
            NormalizedUserName = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            AccountStatus = AccountStatus.Unassigned,
            IsOnboardingComplete = false,
            EmailConfirmed = false,
            CreatedAt = now,
        };

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // RACE-03 — two concurrent registrations for the same email both pass the
            // AnyAsync pre-check above, then race to insert. The unique index on the
            // normalized user name (== email) rejects the loser with a duplicate-key
            // DbUpdateException; translate it to the same friendly 409 the pre-check
            // returns instead of leaking a raw 500.
            throw new ConflictException("An account with this email already exists.");
        }

        // Send the email-verification link (FR-215). Best-effort — email is a side
        // channel; a delivery failure must not break registration.
        await EmailVerificationSender.SendAsync(
            user, emailVerification, emailService, appOptions.Value.ClientUrl, ct);

        var tokens = tokenService.IssueTokens(user, roles: [], activeRole: null, request.RememberMe);
        return AuthDtoFactory.Build(tokens, user, roles: []);
    }
}
