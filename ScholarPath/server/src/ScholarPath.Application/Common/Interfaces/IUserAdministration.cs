using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Common.Interfaces;

public interface IUserAdministration
{
    Task<bool> SetAccountStatusAsync(Guid userId, AccountStatus status, string? reason, CancellationToken ct);
    Task<bool> SoftDeleteAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct);
    Task<bool> AddRoleAsync(Guid userId, string role, CancellationToken ct);
    Task<bool> RemoveRoleAsync(Guid userId, string role, CancellationToken ct);
    Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken ct);
}
