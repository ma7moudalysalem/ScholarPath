using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// ASP.NET Identity-backed implementation of <see cref="IEmailChangeService"/>
/// (FR-231). Wraps <c>UserManager</c> so the Application layer can drive the
/// change-email confirmation flow without referencing Identity directly.
/// </summary>
public sealed class EmailChangeService(
    UserManager<ApplicationUser> users,
    ILogger<EmailChangeService> logger) : IEmailChangeService
{
    public async Task<string> GenerateChangeEmailTokenAsync(Guid userId, string newEmail, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User {userId} was not found.");

        return await users.GenerateChangeEmailTokenAsync(user, newEmail).ConfigureAwait(false);
    }

    public async Task<bool> ConfirmEmailChangeAsync(Guid userId, string newEmail, string token, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return false;

        var result = await users.ChangeEmailAsync(user, newEmail, token).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Change-email confirmation failed for {UserId}: {Errors}",
                userId, string.Join(";", result.Errors.Select(e => e.Description)));
            return false;
        }

        // Keep the login name in sync with the new email and invalidate
        // outstanding security-stamp-derived tokens.
        var setUserName = await users.SetUserNameAsync(user, newEmail).ConfigureAwait(false);
        if (!setUserName.Succeeded)
        {
            logger.LogWarning(
                "Change-email succeeded but SetUserName failed for {UserId}: {Errors}",
                userId, string.Join(";", setUserName.Errors.Select(e => e.Description)));
        }

        await users.UpdateSecurityStampAsync(user).ConfigureAwait(false);
        return true;
    }
}
