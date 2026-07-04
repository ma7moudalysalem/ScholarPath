using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Analytics.Queries.GetConsultantKpis;

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

public record GetConsultantKpisQuery : IRequest<ConsultantKpisDto>;

public sealed class GetConsultantKpisQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetConsultantKpisQuery, ConsultantKpisDto>
{
    public async Task<ConsultantKpisDto> Handle(
        GetConsultantKpisQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(b => b.ConsultantId == userId && !b.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Completed = g.Count(b => b.Status == BookingStatus.Completed),
                Cancelled = g.Count(b => b.Status == BookingStatus.Cancelled),
                Rejected = g.Count(b => b.Status == BookingStatus.Rejected),
                ConsultantNoShows = g.Count(b => b.IsNoShowConsultant),
                StudentNoShows = g.Count(b => b.IsNoShowStudent),
                CompletedRevenueUsd = g
                    .Where(b => b.Status == BookingStatus.Completed)
                    .Sum(b => (decimal?)b.PriceUsd) ?? 0m,
            })
            .FirstOrDefaultAsync(ct);

        // PB-006R: read the penalized rating snapshot off UserProfile rather than
        // live-aggregating raw reviews, so the KPI card reflects reputation penalties.
        var reviews = await db.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                Count = p.ConsultantReviewCount,
                Avg = p.ConsultantAverageRating,
            })
            .FirstOrDefaultAsync(ct);

        return new ConsultantKpisDto(
            TotalBookings:        bookings?.Total ?? 0,
            CompletedBookings:    bookings?.Completed ?? 0,
            CancelledBookings:    bookings?.Cancelled ?? 0,
            RejectedBookings:     bookings?.Rejected ?? 0,
            ConsultantNoShows:    bookings?.ConsultantNoShows ?? 0,
            StudentNoShows:       bookings?.StudentNoShows ?? 0,
            CompletedRevenueUsd:  bookings?.CompletedRevenueUsd ?? 0m,
            ReviewCount:          reviews?.Count ?? 0,
            AverageRating:        reviews?.Avg);
    }
}
