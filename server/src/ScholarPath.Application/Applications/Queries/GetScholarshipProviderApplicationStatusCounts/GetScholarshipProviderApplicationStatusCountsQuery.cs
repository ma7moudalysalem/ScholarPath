using MediatR;

namespace ScholarPath.Application.Applications.Queries.GetScholarshipProviderApplicationStatusCounts;

/// <summary>
/// Returns the count of the authenticated provider's submitted applications grouped
/// by status, computed over ALL of them (no paging). The dashboard's "Pending review"
/// KPI and "Applications by status" chart derive from this so they stay correct for
/// providers with more applications than a single page can hold.
/// </summary>
public sealed record GetScholarshipProviderApplicationStatusCountsQuery
    : IRequest<ScholarshipProviderApplicationStatusCountsDto>;

/// <summary>Total submitted applications plus the per-status breakdown (status name → count).</summary>
public sealed record ScholarshipProviderApplicationStatusCountsDto(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus);
