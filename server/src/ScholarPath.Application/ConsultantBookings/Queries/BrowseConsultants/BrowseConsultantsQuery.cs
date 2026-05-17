using MediatR;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;

namespace ScholarPath.Application.ConsultantBookings.Queries.BrowseConsultants;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists every active consultant with a profile summary for the public browse
/// page. Anonymous-accessible. Delegates to <see cref="IConsultantReadService"/>
/// because identifying <c>Consultant</c>-role users needs the Identity
/// join-tables, which are not exposed on <see cref="IApplicationDbContext"/>.
/// </summary>
public sealed record BrowseConsultantsQuery : IRequest<IReadOnlyList<ConsultantSummaryDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class BrowseConsultantsQueryHandler(IConsultantReadService consultants)
    : IRequestHandler<BrowseConsultantsQuery, IReadOnlyList<ConsultantSummaryDto>>
{
    public Task<IReadOnlyList<ConsultantSummaryDto>> Handle(
        BrowseConsultantsQuery request, CancellationToken ct)
        => consultants.BrowseConsultantsAsync(ct);
}
