using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Payments.Commands.CapturePaymentIntent;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Captures a held Stripe PaymentIntent (manual capture flow).
/// Used when consultant accepts a booking — funds move from hold to captured.
/// Idempotent: returns false if no Held payment exists (already processed).
/// </summary>
[Auditable(AuditAction.PaymentCaptured, "Payment",
    TargetIdProperty = nameof(PaymentId),
    SummaryTemplate = "Captured payment {PaymentId}")]
public sealed record CapturePaymentIntentCommand(Guid PaymentId) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class CapturePaymentIntentCommandValidator
    : AbstractValidator<CapturePaymentIntentCommand>
{
    public CapturePaymentIntentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("PaymentId is required.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class CapturePaymentIntentCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    ILogger<CapturePaymentIntentCommandHandler> logger)
    : IRequestHandler<CapturePaymentIntentCommand, bool>
{
    public async Task<bool> Handle(
        CapturePaymentIntentCommand request,
        CancellationToken ct)
    {
        // 1. Load Held payment — only Held can be captured
        var payment = await db.Payments
            .FirstOrDefaultAsync(p =>
                p.Id == request.PaymentId &&
                p.Status == PaymentStatus.Held, ct);

        // Idempotent: already captured/refunded/cancelled
        if (payment is null)
        {
            logger.LogInformation(
                "No Held payment found for PaymentId {PaymentId}. Already processed or never created.",
                request.PaymentId);
            return false;
        }

        // 2. Deterministic idempotency key
        var idempotencyKey = $"capture:{payment.Id:N}";

        // 3. Call Stripe
        var stripeResult = await stripeService.CapturePaymentIntentAsync(
            paymentIntentId: payment.StripePaymentIntentId!,
            amountToCaptureCents: null, // capture full held amount
            idempotencyKey: idempotencyKey,
            ct: ct);

        // 4. Guard: non-success status is a hard failure — throw so caller can retry
        if (stripeResult.Status != "succeeded")
        {
            logger.LogWarning(
                "Stripe capture for payment {PaymentId} returned non-success status: {Status}",
                payment.Id, stripeResult.Status);

            throw new ConflictException(
                $"Stripe capture did not succeed. Status: {stripeResult.Status}");
        }

        // 5. Update Payment row
        payment.Status = PaymentStatus.Captured;
        payment.CapturedAt = DateTimeOffset.UtcNow;

        if (stripeResult.LatestChargeId is not null)
            payment.StripeChargeId = stripeResult.LatestChargeId;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Successfully captured payment {PaymentId} (intent={IntentId})",
            payment.Id, payment.StripePaymentIntentId);

        return true;
    }
}
