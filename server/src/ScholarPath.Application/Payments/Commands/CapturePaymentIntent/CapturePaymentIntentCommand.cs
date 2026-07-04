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
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

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
    ICurrentUserService currentUser,
    ILogger<CapturePaymentIntentCommandHandler> logger)
    : IRequestHandler<CapturePaymentIntentCommand, bool>
{
    public async Task<bool> Handle(
        CapturePaymentIntentCommand request,
        CancellationToken ct)
    {
        // SEC-01: manual capture moves money — administrators only (Admin or the
        // higher-privilege SuperAdmin, matching every other admin-gated path). The
        // legitimate capture-on-accept path runs server-side in
        // AcceptBookingCommandHandler by calling IStripeService directly and never
        // routes through this command, so this HTTP-facing command is an admin/ops
        // path. Without this gate any authenticated user could capture any Held
        // payment by guessing its id.
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can capture a payment.");

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

        // PB-014 AC#3/#4: lock in the platform split at capture time from the
        // rule in force right now (FR-163..176). This snapshot is immutable and
        // is exactly what payouts pay.
        var split = await FinancialRuleResolver
            .ResolvePaymentSplitAsync(db, payment.Type, payment.AmountCents, ct);
        payment.ProfitShareAmountCents = split.PlatformTakeCents;
        payment.PayeeAmountCents = split.PayeeNetCents;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Successfully captured payment {PaymentId} (intent={IntentId})",
            payment.Id, payment.StripePaymentIntentId);

        return true;
    }
}
