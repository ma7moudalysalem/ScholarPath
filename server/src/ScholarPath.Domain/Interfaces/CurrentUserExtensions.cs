namespace ScholarPath.Domain.Interfaces;

/// <summary>
/// Role helpers on <see cref="ICurrentUserService"/>.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// True when the caller's ACTIVE role is Admin or SuperAdmin. SuperAdmin has every
    /// Admin capability, so admin gates must accept both. Because the JWT
    /// <c>RoleClaimType</c> is <c>active_role</c> (the single session role), a bare
    /// <c>IsInRole("Admin")</c> spuriously 403s a session acting as SuperAdmin — use this
    /// everywhere an admin capability is gated.
    /// </summary>
    public static bool IsAdminOrSuperAdmin(this ICurrentUserService user) =>
        user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
}
