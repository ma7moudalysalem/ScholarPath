using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Payments.Commands.ProcessStripeWebhook;

public sealed record ProcessStripeWebhookCommand(
    string EventId,
    string EventType,
    string? PaymentIntentId,
    string? ChargeId,
    long? AmountCents,
    string DataJson,
    string? ConnectAccountId = null,
    bool? ConnectPayoutsEnabled = null,
    string? PayoutId = null,
    string? PayoutFailureMessage = null,
    string? DisputeReason = null) : IRequest<bool>;

public sealed class ProcessStripeWebhookCommandHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
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
        var matchedConnect = await ApplyConnectOrPayoutAsync(request, ct);

        if (!matchedReview && matchedPayment is null && !matchedConnect)
        {
            logger.LogInformation(
                "Webhook {EventId} ({Type}) matched no record (intent {IntentId}, charge {ChargeId}).",
                request.EventId, request.EventType, request.PaymentIntentId, request.ChargeId);
        }

        webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
        webhookEvent.IsProcessed = true;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (matchedPayment is not null && request.EventType == "charge.dispute.created")
            await NotifyAdminsOfDisputeAsync(request, ct);

        // FR-194 (PB-006 gap P18): a captured payment sends the payer a receipt;
        // a refund event sends a refund notice. Both in-app + email, idempotent.
        if (matchedPayment is not null)
            await DispatchPaymentNotificationAsync(matchedPayment, request.EventType, ct);

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
    private async Task<Payment?> ApplyToPaymentAsync(
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

        if (payment is null) return null;

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

            case "charge.dispute.created":
                payment.Status = PaymentStatus.Disputed;
                payment.FailureReason = request.DisputeReason ?? "charge.dispute.created";
                break;
        }

        return payment;
    }

    // PB-013 — Connect onboarding + payout lifecycle. account.updated drives payee
    // verification; payout.paid / payout.failed finalize a Payout row.
    private async Task<bool> ApplyConnectOrPayoutAsync(
        ProcessStripeWebhookCommand request, CancellationToken ct)
    {
        switch (request.EventType)
        {
            case "account.updated":
                if (string.IsNullOrEmpty(request.ConnectAccountId)) return false;

                var profile = await db.UserProfiles
                    .FirstOrDefaultAsync(p => p.StripeConnectAccountId == request.ConnectAccountId, ct);
                if (profile is null) return false;

                var verified = request.ConnectPayoutsEnabled == true;
                profile.StripeConnectStatus = verified
                    ? StripeConnectStatus.Verified
                    : StripeConnectStatus.Pending;
                if (verified && profile.StripeConnectOnboardedAt is null)
                    profile.StripeConnectOnboardedAt = DateTimeOffset.UtcNow;

                logger.LogInformation(
                    "Connect account {AccountId} -> {Status} via webhook.",
                    request.ConnectAccountId, profile.StripeConnectStatus);
                return true;

            case "payout.paid":
            case "payout.failed":
                if (string.IsNullOrEmpty(request.PayoutId)) return false;

                var payout = await db.Payouts
                    .FirstOrDefaultAsync(p => p.StripePayoutId == request.PayoutId, ct);
                if (payout is null) return false;

                if (request.EventType == "payout.paid")
                {
                    payout.Status = PayoutStatus.Paid;
                    payout.PaidAt ??= DateTimeOffset.UtcNow;
                }
                else
                {
                    payout.Status = PayoutStatus.Failed;
                    var reason = request.PayoutFailureMessage ?? "payout.failed";
                    payout.FailureReason = reason.Length > 500 ? reason[..500] : reason;
                }

                logger.LogInformation(
                    "Payout {PayoutId} -> {Status} via webhook.",
                    request.PayoutId, payout.Status);
                return true;

            default:
                return false;
        }
    }

    // Task 5A — a card dispute needs human action; alert every active admin.
    private async Task NotifyAdminsOfDisputeAsync(
        ProcessStripeWebhookCommand request, CancellationToken ct)
    {
        try
        {
            var adminIds = await db.Users
                .Where(u => u.ActiveRole == "Admin" && u.AccountStatus == AccountStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(ct);
            if (adminIds.Count == 0) return;

            var reason = request.DisputeReason ?? "unspecified";
            await notifications.DispatchBroadcastAsync(
                adminIds,
                NotificationType.PaymentDisputed,
                new NotificationParams { Reason = reason },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to notify admins of the dispute for event {EventId}.", request.EventId);
        }
    }

    // FR-194 — payment-lifecycle notifications. The payer always hears about a
    // hold, a capture (receipt), or a refund; the payee also hears about a
    // capture so they know money landed on their balance.
    private async Task DispatchPaymentNotificationAsync(
        Payment payment, string eventType, CancellationToken ct)
    {
        switch (eventType)
        {
            case "payment_intent.amount_capturable_updated":
                await SafeDispatchAsync(
                    payment.PayerUserId,
                    NotificationType.PaymentHeld,
                    payment.AmountCents, payment.Currency,
                    idempotencyKey: $"payment-held:{payment.Id:N}",
                    eventType, payment.Id, ct);
                break;

            case "payment_intent.succeeded":
                await SafeDispatchAsync(
                    payment.PayerUserId,
                    NotificationType.PaymentSuccess,
                    payment.AmountCents, payment.Currency,
                    idempotencyKey: $"payment-receipt:{payment.Id:N}",
                    eventType, payment.Id, ct);

                if (payment.PayeeUserId is { } payeeId)
                {
                    await SafeDispatchAsync(
                        payeeId,
                        NotificationType.PaymentReceived,
                        payment.PayeeAmountCents, payment.Currency,
                        idempotencyKey: $"payment-received:{payment.Id:N}",
                        eventType, payment.Id, ct);
                }
                break;

            case "charge.refunded":
                await SafeDispatchAsync(
                    payment.PayerUserId,
                    NotificationType.PaymentRefunded,
                    payment.RefundedAmountCents, payment.Currency,
                    idempotencyKey: $"payment-refund:{payment.Id:N}",
                    eventType, payment.Id, ct);
                break;
        }
    }

    private async Task SafeDispatchAsync(
        Guid recipientId, NotificationType type, long amountCents, string currency,
        string idempotencyKey, string eventType, Guid paymentId, CancellationToken ct)
    {
        try
        {
            var amountText = $"{currency} {amountCents / 100m:0.00}";
            await notifications.DispatchAsync(
                recipientId,
                type,
                new NotificationParams { AmountText = amountText },
                deepLink: null,
                idempotencyKey: idempotencyKey,
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to send the {EventType} notification ({Type}) for payment {PaymentId}.",
                eventType, type, paymentId);
        }
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
