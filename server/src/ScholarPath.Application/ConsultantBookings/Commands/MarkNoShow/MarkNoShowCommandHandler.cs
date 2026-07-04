using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;

/// <summary>
/// PB-006R (FR-CBR-25..32): reporting a no-show no longer applies penalties
/// directly. It files a <see cref="NoShowReport"/> (PendingReview), freezes the
/// booking (<see cref="BookingStatus.NoShowReported"/>), and notifies admins. All
/// penalties (blocks, rating deductions, refunds) are applied only when an admin
/// validates the report via ResolveNoShowReport.
/// </summary>
public sealed class MarkNoShowCommandHandler : IRequestHandler<MarkNoShowCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationDispatcher _notifications;
    private readonly ILogger<MarkNoShowCommandHandler> _logger;

    public MarkNoShowCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        INotificationDispatcher notifications,
        ILogger<MarkNoShowCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task Handle(MarkNoShowCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var currentUserId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingDomainException("Booking was not found.");
        }

        var isStudent = booking.StudentId == currentUserId;
        var isConsultant = booking.ConsultantId == currentUserId;

        if (!isStudent && !isConsultant)
        {
            throw new UnauthorizedAccessException("You are not allowed to mark no-show for this booking.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new BookingDomainException("Only confirmed bookings can be reported as no-show.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var sessionEndUtc = booking.ScheduledEndAt.ToUniversalTime();

        if (nowUtc < sessionEndUtc)
        {
            throw new BookingDomainException("No-show can only be reported after the session end time.");
        }

        if (nowUtc > sessionEndUtc.AddHours(6))
        {
            throw new BookingDomainException("No-show can only be reported within 6 hours after session end.");
        }

        // The reporter accuses the OTHER party. Student → accuses consultant, and
        // vice-versa. AccusedRole records which side the accused held.
        var (accusedUserId, accusedRole) = isStudent
            ? (booking.ConsultantId, NoShowAccusedRole.Consultant)
            : (booking.StudentId, NoShowAccusedRole.Student);

        var alreadyReported = await _context.NoShowReports
            .AnyAsync(r => r.BookingId == booking.Id && r.AccusedUserId == accusedUserId, cancellationToken);

        if (alreadyReported)
        {
            throw new BookingDomainException("A no-show report for this booking is already pending review.");
        }

        _context.NoShowReports.Add(new NoShowReport
        {
            BookingId = booking.Id,
            ReporterUserId = currentUserId,
            AccusedUserId = accusedUserId,
            AccusedRole = accusedRole,
            Status = NoShowReportStatus.PendingReview,
            ReporterNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        });

        // Freeze the booking pending admin validation — no refund, no penalty yet.
        booking.NoShowMarkedAt = nowUtc;
        booking.Status = BookingStatus.NoShowReported;

        await _context.SaveChangesAsync(cancellationToken);

        await NotifyAdminsAsync(booking.Id, accusedRole, cancellationToken);
    }

    private async Task NotifyAdminsAsync(Guid bookingId, NoShowAccusedRole accusedRole, CancellationToken ct)
    {
        try
        {
            var adminIds = await _context.Users
                .Where(u => (u.ActiveRole == "Admin" || u.ActiveRole == "SuperAdmin")
                            && u.AccountStatus == AccountStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(ct);

            foreach (var adminId in adminIds)
            {
                try
                {
                    await _notifications.DispatchAsync(
                        adminId,
                        NotificationType.NoShowReportSubmitted,
                        new NotificationParams { Reason = accusedRole.ToString() },
                        deepLink: "/admin/no-show-reports",
                        idempotencyKey: $"noshow-report:{bookingId:N}:{accusedRole}:{adminId:N}",
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to dispatch NoShowReportSubmitted to admin {AdminId} for booking {BookingId}.",
                        adminId, bookingId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not resolve admin recipients for NoShowReportSubmitted on booking {BookingId}.",
                bookingId);
        }
    }
}
