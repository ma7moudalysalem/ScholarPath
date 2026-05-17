using ScholarPath.Application.ConsultantBookings.DTOs;

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Read projections for the public consultant marketplace (PB-006 browse /
/// detail / open-slots). Kept separate from <see cref="IApplicationDbContext"/>
/// for the same reason as <see cref="IAdminReadService"/>: identifying who is a
/// <c>Consultant</c> needs the Identity join-tables (<c>AspNetUserRoles</c> /
/// <c>AspNetRoles</c>), which must not leak into the Application layer.
/// Implementation lives in Infrastructure where they are accessible.
/// </summary>
public interface IConsultantReadService
{
    /// <summary>
    /// Lists every active user in the <c>Consultant</c> role with a profile
    /// summary (name, photo, expertise, rating, fee, availability summary).
    /// Anonymous-accessible.
    /// </summary>
    Task<IReadOnlyList<ConsultantSummaryDto>> BrowseConsultantsAsync(CancellationToken ct);

    /// <summary>
    /// Returns one consultant's full profile detail, or <see langword="null"/>
    /// when the id is not an active user in the <c>Consultant</c> role.
    /// Anonymous-accessible.
    /// </summary>
    Task<ConsultantDetailDto?> GetConsultantDetailAsync(Guid consultantId, CancellationToken ct);

    /// <summary>
    /// Returns the consultant's upcoming bookable slots — recurring + ad-hoc
    /// availability expanded into concrete dated windows, minus windows already
    /// taken by a non-cancelled booking. Returns <see langword="null"/> when the
    /// id is not an active consultant. Anonymous-accessible.
    /// </summary>
    Task<IReadOnlyList<BookableSlotDto>?> GetConsultantOpenSlotsAsync(
        Guid consultantId,
        CancellationToken ct);
}
