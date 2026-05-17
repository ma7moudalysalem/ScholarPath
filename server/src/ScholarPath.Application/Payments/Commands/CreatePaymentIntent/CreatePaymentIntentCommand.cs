using System;
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ProfitShare;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Payments.Commands.CreatePaymentIntent;

// ─── Result ───────────────────────────────────────────────────────────────────

public sealed record CreatePaymentIntentResult(
    Guid PaymentId,
    string ClientSecret,
    string PaymentIntentId);

// ─── Command ──────────────────────────────────────────────────────────────────
// PayerUserId is intentionally NOT a request field — the payer is always the
// authenticated caller, resolved server-side (prevents charging on behalf of others).

[Auditable(AuditAction.Create, "Payment",
    SummaryTemplate = "Created payment intent for {Type}")]
public sealed record CreatePaymentIntentCommand(
    PaymentType Type,
    long AmountCents,
    string Currency,
    Guid? PayeeUserId,
    Guid? RelatedBookingId,
    Guid? RelatedApplicationId,
    IDictionary<string, string>? Metadata = null) : IRequest<CreatePaymentIntentResult>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class CreatePaymentIntentCommandValidator
    : AbstractValidator<CreatePaymentIntentCommand>
{
    public CreatePaymentIntentCommandValidator()
    {
        RuleFor(x => x.AmountCents)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(3)
            .WithMessage("Currency must be a 3-letter ISO code (e.g. USD).");

        RuleFor(x => x.RelatedBookingId)
            .NotEmpty()
            .When(x => x.Type == PaymentType.ConsultantBooking)
            .WithMessage("RelatedBookingId is required for ConsultantBooking payments.");

        RuleFor(x => x.RelatedApplicationId)
            .NotEmpty()
            .When(x => x.Type == PaymentType.CompanyReview)
            .WithMessage("RelatedApplicationId is required for CompanyReview payments.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class CreatePaymentIntentCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    ICurrentUserService currentUser,
    ILogger<CreatePaymentIntentCommandHandler> logger)
    : IRequestHandler<CreatePaymentIntentCommand, CreatePaymentIntentResult>
{
    public async Task<CreatePaymentIntentResult> Handle(
        CreatePaymentIntentCommand request,
        CancellationToken ct)
    {
        // The payer is the authenticated caller — never a client-supplied id.
        var payerUserId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // 1. Resolve profit-share percentage from the config in force right now.
        var platformPct = await ProfitShareConfigResolver
            .ResolveActivePercentageAsync(db, request.Type, ct);
        var breakdown = ProfitShareCalculator.Calculate(request.AmountCents, platformPct);

        // 2. Capture method per type
        var captureMethod = request.Type == PaymentType.ConsultantBooking
            ? "manual"      // hold until consultant accepts
            : "automatic";  // charge immediately for company reviews

        // 3. Deterministic idempotency key
        var relatedId = (request.RelatedBookingId ?? request.RelatedApplicationId)!.Value;
        var idempotencyKey = $"create-intent:{request.Type}:{payerUserId:N}:{relatedId:N}";

        // 4. Guard: return existing if already created successfully
        var existing = await db.Payments.FirstOrDefaultAsync(p =>
            p.IdempotencyKey == idempotencyKey &&
            p.Status != PaymentStatus.Failed &&
            p.Status != PaymentStatus.Cancelled, ct);

        if (existing is not null)
        {
            logger.LogInformation(
                "Payment intent already exists for key {Key}. Returning existing {PaymentId}.",
                idempotencyKey, existing.Id);

            return new CreatePaymentIntentResult(
                existing.Id,
                $"cs_existing_{existing.StripePaymentIntentId}",
                existing.StripePaymentIntentId!);
        }

        // 5. Call Stripe
        var stripeResult = await stripeService.CreatePaymentIntentAsync(
            amountCents: request.AmountCents,
            currency: request.Currency.ToLower(),
            captureMethod: captureMethod,
            metadata: request.Metadata,
            idempotencyKey: idempotencyKey,
            ct: ct);

        if (stripeResult.ClientSecret is null)
            throw new ConflictException(
                $"Stripe did not return a ClientSecret. Status: {stripeResult.Status}");

        // 6. Persist Payment row
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Status = PaymentStatus.Pending,
            AmountCents = request.AmountCents,
            Currency = request.Currency.ToUpper(),
            ProfitShareAmountCents = breakdown.ProfitShareAmountCents,
            PayeeAmountCents = breakdown.PayeeAmountCents,
            RefundedAmountCents = 0,
            PayerUserId = payerUserId,
            PayeeUserId = request.PayeeUserId,
            StripePaymentIntentId = stripeResult.Id,
            StripeChargeId = stripeResult.LatestChargeId,
            IdempotencyKey = idempotencyKey,
            RelatedBookingId = request.RelatedBookingId,
            RelatedApplicationId = request.RelatedApplicationId,
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created payment {PaymentId} type={Type} amount={Amount} intent={IntentId}",
            payment.Id, request.Type, request.AmountCents, stripeResult.Id);

        return new CreatePaymentIntentResult(
            payment.Id,
            stripeResult.ClientSecret,
            stripeResult.Id);
    }
}
