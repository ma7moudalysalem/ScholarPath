using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Queries.GetAnalyticsOverview;

public sealed class GetAnalyticsOverviewQueryHandler(
    IApplicationDbContext db,
    IDateTimeService clock)
    : IRequestHandler<GetAnalyticsOverviewQuery, AnalyticsOverviewDto>
{
    public async Task<AnalyticsOverviewDto> Handle(GetAnalyticsOverviewQuery request, CancellationToken ct)
    {
        var since24h = clock.UtcNow.AddHours(-24);

        // Sequential because IApplicationDbContext → single DbContext instance
        // (EF Core forbids parallel queries on the same context).
        var totalUsers = await db.Users.CountAsync(ct).ConfigureAwait(false);
        var activeUsers = await db.Users.CountAsync(u => u.AccountStatus == AccountStatus.Active, ct).ConfigureAwait(false);
        var pendingApprovals = await db.Users.CountAsync(u => u.AccountStatus == AccountStatus.PendingApproval, ct).ConfigureAwait(false);

        var totalScholarships = await db.Scholarships.CountAsync(ct).ConfigureAwait(false);
        var openScholarships = await db.Scholarships
            .CountAsync(s => s.Status == ScholarshipStatus.Open, ct).ConfigureAwait(false);

        var totalApplications = await db.Applications.CountAsync(ct).ConfigureAwait(false);
        var submittedApplications = await db.Applications
            .CountAsync(a => a.Status != ApplicationStatus.Draft, ct).ConfigureAwait(false);

        var totalBookings = await db.Bookings.CountAsync(ct).ConfigureAwait(false);
        var completedBookings = await db.Bookings
            .CountAsync(b => b.Status == BookingStatus.Completed, ct).ConfigureAwait(false);

        var captured = await db.Payments
            .Where(p => p.Status == PaymentStatus.Captured)
            .Select(p => (long?)p.AmountCents)
            .SumAsync(ct).ConfigureAwait(false);

        var profitShare = await db.Payments
            .Where(p => p.Status == PaymentStatus.Captured)
            .Select(p => (long?)p.ProfitShareAmountCents)
            .SumAsync(ct).ConfigureAwait(false);

        var ai24h = await db.AiInteractions.CountAsync(x => x.CreatedAt >= since24h, ct).ConfigureAwait(false);

        return new AnalyticsOverviewDto(
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            PendingApprovals: pendingApprovals,
            TotalScholarships: totalScholarships,
            OpenScholarships: openScholarships,
            TotalApplications: totalApplications,
            SubmittedApplications: submittedApplications,
            TotalBookings: totalBookings,
            CompletedBookings: completedBookings,
            RevenueCentsCaptured: captured ?? 0,
            ProfitShareCentsAccumulated: profitShare ?? 0,
            AiInteractions24h: ai24h);
    }
}
