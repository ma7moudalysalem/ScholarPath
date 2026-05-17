using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Email verification (FR-215) on top of ASP.NET Identity's built-in
/// email-confirmation token provider. Identity's <see cref="UserManager{T}"/>
/// owns both token generation and validation — there is no separate token row.
/// </summary>
public sealed class EmailVerificationService(
    UserManager<ApplicationUser> users,
    ILogger<EmailVerificationService> logger) : IEmailVerificationService
{
    public async Task<string?> GenerateConfirmationTokenAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return null;

        return await users.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
    }

    public async Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return false;

        if (user.EmailConfirmed)
            return true;

        var result = await users.ConfirmEmailAsync(user, token).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            logger.LogWarning("Email confirmation failed for {UserId}: {Errors}",
                userId, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return result.Succeeded;
    }
}
