using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Services;

/// <summary>
/// Pure helpers for the student booking-access block (PB-006R, FR-CBR-21..24).
/// Operates on the tracked <see cref="UserProfile"/> so the caller keeps a single
/// unit of work — no DB access of its own.
/// </summary>
public static class BookingBlockService
{
    /// <summary>
    /// True when the student is currently blocked from creating new bookings.
    /// A stale <c>BookingBlocked</c> status whose <c>BookingBlockUntil</c> has passed
    /// reads as NOT blocked (lazy expiry) — the student can book again without an
    /// explicit unblock step.
    /// </summary>
    public static bool IsCurrentlyBlocked(UserProfile profile, DateTimeOffset nowUtc) =>
        profile.BookingAccessStatus == BookingAccessStatus.BookingBlocked
        && profile.BookingBlockUntil is { } until
        && until > nowUtc;

    /// <summary>
    /// Applies a booking block for <paramref name="days"/> days. Extend-never-shorten:
    /// if an existing active block already runs longer, it is left untouched so a
    /// shorter later penalty can't reduce a student's outstanding block.
    /// </summary>
    public static void ApplyBlock(
        UserProfile profile, BookingBlockReason reason, int days, DateTimeOffset nowUtc)
    {
        var candidateUntil = nowUtc.AddDays(days);

        if (IsCurrentlyBlocked(profile, nowUtc)
            && profile.BookingBlockUntil is { } existing
            && existing >= candidateUntil)
        {
            // The outstanding block is already longer — don't shorten it.
            return;
        }

        profile.BookingAccessStatus = BookingAccessStatus.BookingBlocked;
        profile.BookingBlockReason = reason;
        profile.BookingBlockUntil = candidateUntil;
    }
}
