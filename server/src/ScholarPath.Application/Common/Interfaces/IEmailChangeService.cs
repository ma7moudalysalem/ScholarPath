namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Application-facing abstraction over ASP.NET Identity's change-email token
/// flow (FR-231). Concrete implementation lives in Infrastructure and uses
/// <c>UserManager.GenerateChangeEmailTokenAsync</c> / <c>ChangeEmailAsync</c>.
/// </summary>
public interface IEmailChangeService
{
    /// <summary>
    /// Generates an Identity change-email confirmation token binding
    /// <paramref name="userId"/> to <paramref name="newEmail"/>.
    /// </summary>
    Task<string> GenerateChangeEmailTokenAsync(Guid userId, string newEmail, CancellationToken ct);

    /// <summary>
    /// Applies a pending email change. Returns <c>true</c> when the token is
    /// valid for the user + new email and the change succeeded.
    /// </summary>
    Task<bool> ConfirmEmailChangeAsync(Guid userId, string newEmail, string token, CancellationToken ct);
}
