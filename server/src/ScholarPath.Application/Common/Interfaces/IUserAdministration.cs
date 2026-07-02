using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Application-facing abstraction over the Identity + refresh-token
/// operations the admin module needs. Concrete impl in Infrastructure.
/// </summary>
public interface IUserAdministration
{
    Task<bool> SetAccountStatusAsync(Guid userId, AccountStatus status, string? reason, CancellationToken ct);

    Task<bool> SoftDeleteAsync(Guid userId, CancellationToken ct);

    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct);

    Task<bool> AddRoleAsync(Guid userId, string role, CancellationToken ct);

    Task<bool> RemoveRoleAsync(Guid userId, string role, CancellationToken ct);

    /// <summary>Revokes all active refresh tokens for a user; called on suspend/deactivate.</summary>
    Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken ct);

    /// <summary>
    /// GAP-2 / FR-AUTH-13 — records an external (SSO) login so the account can be
    /// found by provider identity on the next sign-in. Idempotent.
    /// </summary>
    Task AddExternalLoginAsync(Guid userId, string provider, string providerKey, CancellationToken ct);

    /// <summary>
    /// GAP-2 / FR-AUTH-13 — resolves the user id previously linked to an external
    /// (provider, providerKey) login, or <c>null</c> if none is recorded.
    /// </summary>
    Task<Guid?> FindUserIdByExternalLoginAsync(string provider, string providerKey, CancellationToken ct);
}
