using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeService clock)
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
        await db.SaveChangesAsync(ct);

        var tokens = tokenService.IssueTokens(user, roles: [], activeRole: null, request.RememberMe);
        return AuthDtoFactory.Build(tokens, user, roles: []);
    }
}
