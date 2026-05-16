using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Payments.Commands.ProcessStripeWebhook;

public sealed record ProcessStripeWebhookCommand(
    string EventId,
    string EventType,
    string? PaymentIntentId,
    string? ChargeId,
    long? AmountCents,
    string DataJson) : IRequest<bool>;

public sealed class ProcessStripeWebhookCommandHandler(
    IApplicationDbContext db,
    ILogger<ProcessStripeWebhookCommandHandler> logger)
    : IRequestHandler<ProcessStripeWebhookCommand, bool>
{
    public async Task<bool> Handle(ProcessStripeWebhookCommand request, CancellationToken ct)
    {
        // Idempotency: Stripe retries the same event for up to 3 days.
        if (await db.StripeWebhookEvents.AnyAsync(e => e.StripeEventId == request.EventId, ct))
        {
            logger.LogInformation("Webhook {EventId} already processed, skipping.", request.EventId);
            return true;
        }

        var webhookEvent = new StripeWebhookEvent
        {
            StripeEventId = request.EventId,
            EventType = request.EventType,
            RawPayload = request.DataJson,
        };
        db.StripeWebhookEvents.Add(webhookEvent);

        // A Stripe PaymentIntent belongs to exactly one of the two payment tables:
        // consultant bookings use Payment, company reviews use CompanyReviewPayment.
        var matchedReview = await ApplyToCompanyReviewPaymentAsync(request, ct);
        var matchedPayment = await ApplyToPaymentAsync(request, ct);

        if (!matchedReview && !matchedPayment)
        {
            logger.LogInformation(
                "Webhook {EventId} ({Type}) matched no payment record (intent {IntentId}, charge {ChargeId}).",
                request.EventId, request.EventType, request.PaymentIntentId, request.ChargeId);
        }

        webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
        webhookEvent.IsProcessed = true;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    // PB-005 — company-review payments. Manual-capture intents move Pending -> Held;
    // a refund/cancel event drives the terminal state.
    private async Task<bool> ApplyToCompanyReviewPaymentAsync(
        ProcessStripeWebhookCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.PaymentIntentId)) return false;

        var payment = await db.CompanyReviewPayments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == request.PaymentIntentId, ct);
        if (payment is null) return false;

        switch (request.EventType)
        {
            case "payment_intent.succeeded":
            case "payment_intent.amount_capturable_updated":
                if (payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Held;
                    logger.LogInformation(
                        "CompanyReviewPayment {IntentId} -> Held via webhook.", request.PaymentIntentId);
                }
                break;

            case "payment_intent.payment_failed":
                if (payment.Status is PaymentStatus.Pending or PaymentStatus.Held)
                {
                    payment.Status = PaymentStatus.Failed;
                    logger.LogWarning(
                        "CompanyReviewPayment {IntentId} -> Failed via webhook.", request.PaymentIntentId);
                }
                break;

            case "payment_intent.canceled":
                if (payment.Status is PaymentStatus.Pending or PaymentStatus.Held)
                {
                    payment.Status = PaymentStatus.Cancelled;
                }
                break;

            case "charge.refunded":
                payment.Status = PaymentStatus.Refunded;
                logger.LogInformation(
                    "CompanyReviewPayment {IntentId} -> Refunded via webhook.", request.PaymentIntentId);
                break;
        }

        return true;
    }

    // PB-006 — consultant-booking / generic payments. Keeps the Payment row and the
    // owning Booking in sync with Stripe (previously dropped: this path had no live handler).
    private async Task<bool> ApplyToPaymentAsync(
        ProcessStripeWebhookCommand request, CancellationToken ct)
    {
        Payment? payment = null;

        if (!string.IsNullOrEmpty(request.PaymentIntentId))
        {
            payment = await db.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == request.PaymentIntentId, ct);
        }

        if (payment is null && !string.IsNullOrEmpty(request.ChargeId))
        {
            payment = await db.Payments
                .FirstOrDefaultAsync(p => p.StripeChargeId == request.ChargeId, ct);
        }

        if (payment is null) return false;

        switch (request.EventType)
        {
            case "payment_intent.amount_capturable_updated":
                payment.Status = PaymentStatus.Held;
                payment.HeldAt ??= DateTimeOffset.UtcNow;
                payment.FailureReason = null;
                break;

            case "payment_intent.succeeded":
                payment.Status = PaymentStatus.Captured;
                payment.CapturedAt ??= DateTimeOffset.UtcNow;
                payment.FailureReason = null;
                if (!string.IsNullOrEmpty(request.ChargeId))
                {
                    payment.StripeChargeId = request.ChargeId;
                }
                await ConfirmBookingAsync(payment.StripePaymentIntentId, ct);
                break;

            case "payment_intent.payment_failed":
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = "payment_intent.payment_failed";
                await CancelBookingAsync(payment.StripePaymentIntentId, ct);
                break;

            case "payment_intent.canceled":
                payment.Status = PaymentStatus.Cancelled;
                payment.FailureReason = "payment_intent.canceled";
                break;

            case "charge.refunded":
                var refunded = request.AmountCents ?? payment.AmountCents;
                payment.RefundedAmountCents = refunded;
                payment.RefundedAt = DateTimeOffset.UtcNow;
                payment.RefundReason = "charge.refunded";
                payment.Status = refunded >= payment.AmountCents
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;
                break;
        }

        return true;
    }

    private async Task ConfirmBookingAsync(string? paymentIntentId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(paymentIntentId)) return;

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.StripePaymentIntentId == paymentIntentId, ct);

        if (booking is { Status: BookingStatus.Requested })
        {
            booking.Status = BookingStatus.Confirmed;
            booking.ConfirmedAt ??= DateTimeOffset.UtcNow;
        }
    }

    private async Task CancelBookingAsync(string? paymentIntentId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(paymentIntentId)) return;

        var booking = await db.Bookings
            .FirstOrDefaultAsync(b => b.StripePaymentIntentId == paymentIntentId, ct);

        if (booking is not null &&
            booking.Status is not (BookingStatus.Cancelled or BookingStatus.Completed
                or BookingStatus.NoShowStudent or BookingStatus.NoShowConsultant))
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt ??= DateTimeOffset.UtcNow;
        }
    }
}
