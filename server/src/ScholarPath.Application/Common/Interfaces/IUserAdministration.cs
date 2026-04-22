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
}
