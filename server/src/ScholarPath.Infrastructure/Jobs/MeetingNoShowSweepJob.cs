using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Jobs;

/// <summary>
/// FR-217 — automated no-show detection. Every 15 minutes it sweeps confirmed
/// bookings whose session has ended and exactly one party joined the room.
///
/// PB-006R (FR-CBR-25): the sweep no longer applies penalties or refunds directly.
/// It FILES a <see cref="NoShowReport"/> (PendingReview) on the absent party — the
/// present party is recorded as the reporter — and freezes the booking
/// (<see cref="BookingStatus.NoShowReported"/>). An admin then validates the report,
/// at which point the blocks / rating deductions / refunds are applied. This keeps
/// the automated path behind the same admin-validation gate as manual reports.
///
/// Bookings where both parties joined are left to <see cref="CompletionJob"/>.
/// Bookings where neither party joined are left untouched.
/// </summary>
public sealed class MeetingNoShowSweepJob : IMeetingNoShowSweepJob
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationDispatcher _notifications;
    private readonly ILogger<MeetingNoShowSweepJob> _logger;

    // Wait past the scheduled end before judging attendance — a late join is
    // still recorded for up to 15 min after end, so 20 min is safely clear.
    private static readonly TimeSpan PostSessionGrace = TimeSpan.FromMinutes(20);

    public MeetingNoShowSweepJob(
        IApplicationDbContext context,
        INotificationDispatcher notifications,
        ILogger<MeetingNoShowSweepJob> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var endedBefore = nowUtc - PostSessionGrace;

        // No lower age bound: the sweep is the SOLE authority for one-party-joined
        // bookings (CompletionJob now skips them). Without covering any age, a sweep
        // outage longer than the old 6h window would strand a one-party-joined booking
        // in Confirmed forever — CompletionJob won't complete it and the old window
        // wouldn't re-catch it. The filter (Confirmed + exactly-one-joined) is naturally
        // small, so scanning older rows is cheap.
        var bookings = await _context.Bookings
            .Where(b =>
                b.Status == BookingStatus.Confirmed &&
                !b.IsNoShowStudent &&
                !b.IsNoShowConsultant &&
                b.ScheduledEndAt <= endedBefore &&
                // exactly one party joined the room
                ((b.StudentJoinedAt != null && b.ConsultantJoinedAt == null) ||
                 (b.StudentJoinedAt == null && b.ConsultantJoinedAt != null)))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (bookings.Count == 0)
        {
            _logger.LogInformation("MeetingNoShowSweepJob found no bookings to evaluate.");
            return;
        }

        var reported = new List<(Guid BookingId, NoShowAccusedRole Role)>();
        foreach (var booking in bookings)
        {
            try
            {
                // The PRESENT party is the reporter; the ABSENT party is the accused.
                var (reporterId, accusedId, accusedRole) = booking.ConsultantJoinedAt is null
                    ? (booking.StudentId, booking.ConsultantId, NoShowAccusedRole.Consultant)
                    : (booking.ConsultantId, booking.StudentId, NoShowAccusedRole.Student);

                var alreadyReported = await _context.NoShowReports
                    .AnyAsync(r => r.BookingId == booking.Id && r.AccusedUserId == accusedId, ct)
                    .ConfigureAwait(false);
                if (alreadyReported)
                {
                    continue;
                }

                _context.NoShowReports.Add(new NoShowReport
                {
                    BookingId = booking.Id,
                    ReporterUserId = reporterId,
                    AccusedUserId = accusedId,
                    AccusedRole = accusedRole,
                    Status = NoShowReportStatus.PendingReview,
                    ReporterNote = "Auto-detected: only one party joined the meeting room.",
                });

                booking.Status = BookingStatus.NoShowReported;
                booking.NoShowMarkedAt = nowUtc;
                reported.Add((booking.Id, accusedRole));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "MeetingNoShowSweepJob failed to file a report for booking {BookingId}.", booking.Id);
            }
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("MeetingNoShowSweepJob filed {Count} no-show report(s) for admin validation.", reported.Count);

        if (reported.Count > 0)
        {
            await NotifyAdminsAsync(reported, ct).ConfigureAwait(false);
        }
    }

    private async Task NotifyAdminsAsync(
        IReadOnlyCollection<(Guid BookingId, NoShowAccusedRole Role)> reported, CancellationToken ct)
    {
        try
        {
            var adminIds = await _context.Users
                .Where(u => (u.ActiveRole == "Admin" || u.ActiveRole == "SuperAdmin")
                            && u.AccountStatus == AccountStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var (bookingId, role) in reported)
            {
                foreach (var adminId in adminIds)
                {
                    try
                    {
                        await _notifications.DispatchAsync(
                            adminId,
                            NotificationType.NoShowReportSubmitted,
                            new NotificationParams { Reason = role.ToString() },
                            deepLink: "/admin/no-show-reports",
                            idempotencyKey: $"noshow-report:{bookingId:N}:{role}:{adminId:N}",
                            ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to dispatch NoShowReportSubmitted to admin {AdminId} for booking {BookingId}.",
                            adminId, bookingId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MeetingNoShowSweepJob could not resolve admin recipients for notifications.");
        }
    }
}
