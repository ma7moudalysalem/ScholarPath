using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Jobs;

/// <summary>
/// FR-217 — automated no-show detection. Every 15 minutes it sweeps confirmed
/// bookings whose session has ended and exactly one party joined the room:
/// the absent party is marked a no-show. A consultant no-show fully refunds
/// the student (FR-193); a student no-show forfeits the fee (FR-091).
///
/// Bookings where both parties joined are left to <see cref="CompletionJob"/>.
/// Bookings where neither party has a recorded join are left untouched — a
/// missing attendance signal must never produce a false no-show, so the
/// non-absent party can still resolve those manually via MarkNoShow.
/// </summary>
public sealed class MeetingNoShowSweepJob : IMeetingNoShowSweepJob
{
    private readonly IApplicationDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly ILogger<MeetingNoShowSweepJob> _logger;

    // Wait past the scheduled end before judging attendance — a late join is
    // still recorded for up to 15 min after end, so 20 min is safely clear.
    private static readonly TimeSpan PostSessionGrace = TimeSpan.FromMinutes(20);

    // Aligns with MarkNoShowCommandHandler's 6-hour manual window and
    // CompletionJob's 6-hour auto-completion threshold.
    private static readonly TimeSpan NoShowWindow = TimeSpan.FromHours(6);

    public MeetingNoShowSweepJob(
        IApplicationDbContext context,
        IStripeService stripeService,
        ILogger<MeetingNoShowSweepJob> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var endedBefore = nowUtc - PostSessionGrace;
        var endedAfter = nowUtc - NoShowWindow;

        var bookings = await _context.Bookings
            .Include(b => b.Payment)
            .Where(b =>
                b.Status == BookingStatus.Confirmed &&
                !b.IsNoShowStudent &&
                !b.IsNoShowConsultant &&
                b.ScheduledEndAt <= endedBefore &&
                b.ScheduledEndAt > endedAfter &&
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

        var marked = 0;
        foreach (var booking in bookings)
        {
            try
            {
                if (booking.ConsultantJoinedAt is null)
                {
                    await MarkConsultantNoShowAsync(booking, nowUtc, ct).ConfigureAwait(false);
                }
                else
                {
                    MarkStudentNoShow(booking, nowUtc);
                }

                marked++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "MeetingNoShowSweepJob failed to resolve booking {BookingId}.", booking.Id);
            }
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("MeetingNoShowSweepJob marked {Count} no-show booking(s).", marked);
    }

    /// <summary>Consultant absent → the student is fully refunded (FR-193).</summary>
    private async Task MarkConsultantNoShowAsync(
        ConsultantBooking booking, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            // MON-01: refund the amount actually captured — the Payment row is the
            // source of truth — falling back to the PriceUsd derivation only when
            // there is no Payment row. Mirrors MarkNoShowCommandHandler: PriceUsd
            // can drift from the captured amount (post-payment re-price, free-mode
            // toggle, prior partial refund), which would otherwise refund the wrong
            // amount on Stripe while the ledger stamps a full refund.
            var amountCents = booking.Payment is { } capturedPayment
                ? capturedPayment.AmountCents
                : (long)decimal.Round(
                    booking.PriceUsd * 100m, 0, MidpointRounding.AwayFromZero);

            if (amountCents > 0)
            {
                var refund = await _stripeService.RefundPaymentAsync(
                    paymentIntentId: booking.StripePaymentIntentId,
                    amountCents: amountCents,
                    reason: CancellationReason.ConsultantNoShow.ToString(),
                    idempotencyKey: $"booking-noshow-refund:{booking.Id:N}",
                    ct: ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(refund.Id))
                {
                    throw new InvalidOperationException(
                        $"Stripe refund failed for booking {booking.Id}.");
                }

                if (booking.Payment is { } payment)
                {
                    payment.Status = PaymentStatus.Refunded;
                    payment.RefundedAmountCents = payment.AmountCents;
                    payment.RefundedAt = nowUtc;
                    payment.RefundReason = CancellationReason.ConsultantNoShow.ToString();
                }
            }
        }

        booking.IsNoShowConsultant = true;
        booking.Status = BookingStatus.NoShowConsultant;
        booking.CancellationReason = CancellationReason.ConsultantNoShow;
        booking.NoShowMarkedAt = nowUtc;
    }

    /// <summary>Student absent → the session fee is forfeited, no refund (FR-091).</summary>
    private static void MarkStudentNoShow(ConsultantBooking booking, DateTimeOffset nowUtc)
    {
        booking.IsNoShowStudent = true;
        booking.Status = BookingStatus.NoShowStudent;
        booking.CancellationReason = CancellationReason.StudentNoShow;
        booking.NoShowMarkedAt = nowUtc;
    }
}
