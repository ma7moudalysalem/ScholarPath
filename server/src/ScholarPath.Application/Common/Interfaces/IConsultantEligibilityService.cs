namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Single source of truth for "may this user act as a Consultant?".
///
/// Holding the <c>Consultant</c> Identity role is NOT sufficient on its own —
/// a role row can be stale or granted out-of-band. A user may act as a
/// consultant only when ALL of the following hold:
/// <list type="number">
///   <item>they hold the <c>Consultant</c> role,</item>
///   <item>their account is <see cref="Domain.Enums.AccountStatus.Active"/>, and</item>
///   <item>they carry an official approval signal — either
///     <see cref="Domain.Entities.UserProfile.ConsultantVerifiedAt"/> is set,
///     or they have an <see cref="Domain.Enums.UpgradeRequestStatus.Approved"/>
///     Consultant upgrade request.</item>
/// </list>
/// Enforced everywhere consultant capability is exercised (role switch,
/// availability management, the public marketplace) so a single business rule
/// governs them all rather than each handler re-checking role membership.
/// </summary>
public interface IConsultantEligibilityService
{
    /// <summary>
    /// Resolves the user's roles and evaluates full consultant eligibility.
    /// </summary>
    Task<bool> CanActAsConsultantAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Overload for callers that have already loaded the user's roles, to avoid
    /// a redundant role lookup on the hot auth paths.
    /// </summary>
    Task<bool> CanActAsConsultantAsync(
        Guid userId, IReadOnlyList<string> roles, CancellationToken ct);
}
