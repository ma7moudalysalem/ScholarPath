using System;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Payments.Commands.RefundPayment;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Refunds a Held or Captured payment — full or partial. Admin-only.
/// • Full refund   (AmountCents = null): cancels the PaymentIntent if Held,
///                                       or issues a full refund if Captured.
/// • Partial refund (AmountCents > 0):   issues a partial refund via Stripe Refund API.
/// </summary>
[Auditable(AuditAction.PaymentRefunded, "Payment",
    TargetIdProperty = nameof(PaymentId),
    SummaryTemplate = "Refunded payment {PaymentId} — amount={AmountCents}")]
public sealed record RefundPaymentCommand(
    Guid PaymentId,
    long? AmountCents,  // null = full refund
    string? Reason = null) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class RefundPaymentCommandValidator
    : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("PaymentId is required.");

        RuleFor(x => x.AmountCents)
            .GreaterThan(0)
            .When(x => x.AmountCents.HasValue)
            .WithMessage("AmountCents must be greater than zero when specified.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class RefundPaymentCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    ICurrentUserService currentUser,
    ILogger<RefundPaymentCommandHandler> logger)
    : IRequestHandler<RefundPaymentCommand, bool>
{
    public async Task<bool> Handle(
        RefundPaymentCommand request,
        CancellationToken ct)
    {
        // Refunds move money — administrators only.
        if (!currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only an administrator can issue refunds.");

        // 1. Load payment — must be Held or Captured to be refundable
        var payment = await db.Payments
            .FirstOrDefaultAsync(p =>
                p.Id == request.PaymentId &&
                (p.Status == PaymentStatus.Held ||
                 p.Status == PaymentStatus.Captured), ct);

        // Idempotent: already refunded / cancelled / failed
        if (payment is null)
        {
            logger.LogInformation(
                "No refundable payment found for PaymentId {PaymentId}. Already refunded or not in valid state.",
                request.PaymentId);
            return false;
        }

        // 2. Deterministic idempotency key
        var idempotencyKey =
            $"refund:{payment.Id:N}:{request.AmountCents ?? 0}";

        var isFullRefund = request.AmountCents is null ||
                           request.AmountCents >= payment.AmountCents;

        // 3a. Full refund on a Held payment → cancel the PaymentIntent
        //     (releases the card hold without ever charging)
        if (isFullRefund && payment.Status == PaymentStatus.Held)
        {
            var cancelResult = await stripeService.CancelPaymentIntentAsync(
                paymentIntentId: payment.StripePaymentIntentId!,
                cancellationReason: "requested_by_customer",
                idempotencyKey: $"{idempotencyKey}:cancel",
                ct: ct);

            if (cancelResult.Status != "canceled")
            {
                logger.LogWarning(
                    "Stripe cancel for payment {PaymentId} returned unexpected status: {Status}",
                    payment.Id, cancelResult.Status);

                throw new ConflictException(
                    $"Stripe cancellation did not succeed. Status: {cancelResult.Status}");
            }

            payment.Status = PaymentStatus.Refunded;
            payment.RefundedAmountCents = payment.AmountCents;
            payment.RefundedAt = DateTimeOffset.UtcNow;
            payment.RefundReason = request.Reason ?? "Full refund — intent cancelled";
        }
        // 3b. Full or partial refund on a Captured payment → Stripe Refund API
        else
        {
            var refundAmountCents = request.AmountCents ?? payment.AmountCents;

            // Never refund more than what was captured (accounts for prior partial refunds).
            if (payment.RefundedAmountCents + refundAmountCents > payment.AmountCents)
            {
                throw new ConflictException(
                    "Refund would exceed the captured amount of this payment.");
            }

            var refundResult = await stripeService.RefundPaymentAsync(
                paymentIntentId: payment.StripePaymentIntentId!,
                amountCents: refundAmountCents,
                reason: request.Reason ?? "requested_by_customer",
                idempotencyKey: $"{idempotencyKey}:refund",
                ct: ct);

            if (refundResult.Status != "succeeded")
            {
                logger.LogWarning(
                    "Stripe refund for payment {PaymentId} returned status: {Status}",
                    payment.Id, refundResult.Status);

                throw new ConflictException(
                    $"Stripe refund did not succeed. Status: {refundResult.Status}");
            }

            payment.RefundedAmountCents += refundResult.AmountCents;
            payment.RefundedAt = DateTimeOffset.UtcNow;
            payment.RefundReason = request.Reason ?? "Refund processed";

            payment.Status = payment.RefundedAmountCents >= payment.AmountCents
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
        }

        // 4. Persist
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Refund processed for payment {PaymentId} — status={Status}, refunded={Refunded}c",
            payment.Id, payment.Status, payment.RefundedAmountCents);

        return true;
    }
}
