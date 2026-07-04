using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Queries.GetPendingNoShowReports;

/// <summary>
/// Admin queue: no-show reports awaiting validation (PB-006R, FR-CBR-25). One row
/// per PendingReview report, oldest first (so the longest-waiting is triaged
/// first). Backed by the filtered index on NoShowReport.Status.
/// </summary>
public sealed record GetPendingNoShowReportsQuery(
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<NoShowReportRow>>;

public sealed record NoShowReportRow(
    Guid ReportId,
    Guid BookingId,
    string ReporterName,
    string AccusedName,
    NoShowAccusedRole AccusedRole,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    string? ReporterNote,
    DateTimeOffset ReportedAt);

public sealed class GetPendingNoShowReportsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetPendingNoShowReportsQuery, PagedResult<NoShowReportRow>>
{
    public async Task<PagedResult<NoShowReportRow>> Handle(
        GetPendingNoShowReportsQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
        {
            throw new ForbiddenAccessException();
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var pendingQuery =
            from r in db.NoShowReports.AsNoTracking()
            where r.Status == NoShowReportStatus.PendingReview
            join booking in db.Bookings.AsNoTracking() on r.BookingId equals booking.Id
            join reporter in db.Users.AsNoTracking() on r.ReporterUserId equals reporter.Id
            join accused in db.Users.AsNoTracking() on r.AccusedUserId equals accused.Id
            select new { r, booking, reporter, accused };

        var total = await pendingQuery.CountAsync(ct).ConfigureAwait(false);

        var rows = await pendingQuery
            .OrderBy(x => x.r.CreatedAt)
            .ThenBy(x => x.r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NoShowReportRow(
                x.r.Id,
                x.booking.Id,
                (x.reporter.FirstName + " " + x.reporter.LastName).Trim(),
                (x.accused.FirstName + " " + x.accused.LastName).Trim(),
                x.r.AccusedRole,
                x.booking.ScheduledStartAt,
                x.booking.ScheduledEndAt,
                x.r.ReporterNote,
                x.r.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<NoShowReportRow>(rows, page, pageSize, total);
    }
}
