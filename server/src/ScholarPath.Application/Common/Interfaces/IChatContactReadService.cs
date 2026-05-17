using ScholarPath.Application.Chat.DTOs;

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Read projection backing the direct-message compose user-picker. Kept
/// separate from <see cref="IApplicationDbContext"/> for the same reason as
/// <see cref="IConsultantReadService"/>: surfacing a user's role needs the
/// Identity join-tables (<c>AspNetUserRoles</c> / <c>AspNetRoles</c>), which
/// must not leak into the Application layer. Implementation lives in
/// Infrastructure where they are accessible.
/// </summary>
public interface IChatContactReadService
{
    /// <summary>
    /// Lists active users the given user is allowed to message, optionally
    /// filtered by a name search term. Excludes the current user and any user
    /// in a block relationship with them (in either direction). Results are
    /// capped to <paramref name="limit"/> rows.
    /// </summary>
    Task<IReadOnlyList<ChatContactDto>> SearchContactsAsync(
        Guid currentUserId,
        string? query,
        int limit,
        CancellationToken ct);
}
