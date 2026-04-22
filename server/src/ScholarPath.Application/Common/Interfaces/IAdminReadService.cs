using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Admin-scoped read projections. Kept separate from <see cref="IApplicationDbContext"/>
/// so that Identity join-tables (AspNetUserRoles) never leak into the Application layer.
/// Implementation lives in Infrastructure where UserManager + ApplicationDbContext are both accessible.
/// </summary>
public interface IAdminReadService
{
    Task<PagedResult<AdminUserRow>> SearchUsersAsync(
        string? search,
        AccountStatus? status,
        string? role,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<AdminUserDetail?> GetUserDetailAsync(Guid userId, CancellationToken ct);
}
