using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Analytics.Queries.GetConsultantKpis;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record ConsultantKpisDto(
    int TotalBookings,
    int CompletedBookings,
    int CancelledBookings,
    int RejectedBookings,
    int ConsultantNoShows,
    int StudentNoShows,
    decimal CompletedRevenueUsd,
    int ReviewCount,
    decimal? AverageRating);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns KPI aggregates for the calling consultant from <c>dbo.vw_consultant_kpis</c>.
/// Returns an all-zeros DTO when the consultant has no activity yet.
/// </summary>
public record GetConsultantKpisQuery : IRequest<ConsultantKpisDto>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetConsultantKpisQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetConsultantKpisQuery, ConsultantKpisDto>
{
    // Private projection record whose property names match the view column names
    // exactly (SQL Server is case-insensitive for column aliases, but the names
    // must match what EF maps from the result set).
    private sealed record ConsultantKpisRow(
        int TotalBookings,
        int CompletedBookings,
        int CancelledBookings,
        int RejectedBookings,
        int ConsultantNoShows,
        int StudentNoShows,
        decimal CompletedRevenueUsd,
        int ReviewCount,
        decimal? AverageRating);

    public async Task<ConsultantKpisDto> Handle(
        GetConsultantKpisQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var rows = await db.Database
            .SqlQuery<ConsultantKpisRow>(
                $"""
                SELECT
                    TotalBookings,
                    CompletedBookings,
                    CancelledBookings,
                    RejectedBookings,
                    ConsultantNoShows,
                    StudentNoShows,
                    CompletedRevenueUsd,
                    ReviewCount,
                    AverageRating
                FROM dbo.vw_consultant_kpis
                WHERE ConsultantId = {userId}
                """)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return new ConsultantKpisDto(0, 0, 0, 0, 0, 0, 0m, 0, null);

        var r = rows[0];
        return new ConsultantKpisDto(
            r.TotalBookings,
            r.CompletedBookings,
            r.CancelledBookings,
            r.RejectedBookings,
            r.ConsultantNoShows,
            r.StudentNoShows,
            r.CompletedRevenueUsd,
            r.ReviewCount,
            r.AverageRating);
    }
}
