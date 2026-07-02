using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Start;

// ─── Result ───────────────────────────────────────────────────────────────────

/// <summary>
/// Returned to the Student's Apply Now click.
/// <para>
/// For paid scholarships (fee &gt; 0): the frontend uses <see cref="ClientSecret"/>
/// with Stripe Elements to authorise the card (manual capture) and then calls
/// <see cref="ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.ConfirmHold.ConfirmScholarshipProviderReviewRequestHoldCommand"/>
/// to flip the request from Submitted to Pending.
/// </para>
/// <para>
/// For free scholarships (fee = 0): the payment-related fields are <c>null</c>,
/// no Stripe intent is created, and the request is already in <c>Pending</c>
/// — the frontend should just show success without invoking Stripe Elements.
/// <see cref="IsFree"/> makes that branch explicit so the client doesn't have
/// to infer it from a missing client secret.
/// </para>
/// </summary>
public sealed record StartScholarshipProviderReviewRequestResult(
    Guid RequestId,
    bool IsFree,
    Guid? PaymentId,
    string? ClientSecret,
    string? PaymentIntentId,
    long AmountCents,
    string Currency);

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Student-initiated Apply Now (PB-005 final business rule). The Student pays
/// the ScholarshipProvider for application-support / document-review; ScholarPath takes
/// 10% commission from the final retained captured payment, ScholarshipProvider receives
/// 90% — calculated at capture (and re-calculated on partial refund) via
/// <see cref="FinancialRuleResolver"/>.
/// </summary>
[Auditable(AuditAction.Create, "ScholarshipProviderReviewRequest",
    SummaryTemplate = "Started ScholarshipProviderReview request for scholarship {ScholarshipId}")]
public sealed record StartScholarshipProviderReviewRequestCommand(
    Guid ScholarshipId,
    Guid? ApplicationTrackerId = null) : IRequest<StartScholarshipProviderReviewRequestResult>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class StartScholarshipProviderReviewRequestCommandValidator
    : AbstractValidator<StartScholarshipProviderReviewRequestCommand>
{
    public StartScholarshipProviderReviewRequestCommandValidator()
    {
        RuleFor(x => x.ScholarshipId)
            .NotEmpty()
            .WithMessage("ScholarshipId is required.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class StartScholarshipProviderReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    ILogger<StartScholarshipProviderReviewRequestCommandHandler> logger)
    : IRequestHandler<StartScholarshipProviderReviewRequestCommand, StartScholarshipProviderReviewRequestResult>
{
    private const int PendingTtlDays = 7;

    public async Task<StartScholarshipProviderReviewRequestResult> Handle(
        StartScholarshipProviderReviewRequestCommand request,
        CancellationToken ct)
    {
        var studentId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var scholarship = await db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        // ── Business guards ─────────────────────────────────────────────────
        if (scholarship.Mode != ListingMode.InApp)
            throw new ConflictException("Apply Now is only available for in-app scholarship listings.");

        if (scholarship.Status != ScholarshipStatus.Open)
            throw new ConflictException("This scholarship is not currently open for applications.");

        if (scholarship.OwnerScholarshipProviderId is null)
            throw new ConflictException("This scholarship has no ScholarshipProvider owner — Apply Now is not available.");

        if (scholarship.OwnerScholarshipProviderId == studentId)
            throw new ConflictException("A ScholarshipProvider cannot start a paid review request for its own scholarship.");

        // Master switch: when payments are off, treat every request as free
        // regardless of the stored fee — money never moves on this platform
        // until the admin re-enables payments. This also lets legacy listings
        // with NULL ReviewFeeUsd (created before any fee was set) be applied
        // to in free mode, which would otherwise trip the "not configured"
        // guard below.
        var paymentsEnabled = await PlatformSettingsReader.GetBooleanAsync(
            db, PlatformSettingsKeys.PaymentsEnabled, defaultValue: true, ct);

        decimal fee;
        if (!paymentsEnabled)
        {
            fee = 0m;
        }
        else
        {
            if (scholarship.ReviewFeeUsd is not { } storedFee || storedFee < 0m)
                throw new ConflictException(
                    "Review Service Fee is not configured for this scholarship. Apply Now is unavailable.");
            fee = storedFee;
        }

        var companyId = scholarship.OwnerScholarshipProviderId.Value;
        var currency = scholarship.Currency ?? "USD";
        var amountCents = (long)Math.Round(fee * 100m, MidpointRounding.AwayFromZero);
        var isFree = amountCents == 0;

        // ── Idempotency: re-use an in-flight request for the same student/scholarship ─
        var existing = await db.ScholarshipProviderReviewRequests
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r =>
                r.StudentId == studentId &&
                r.ScholarshipId == request.ScholarshipId &&
                (r.Status == ScholarshipProviderReviewRequestStatus.Draft ||
                 r.Status == ScholarshipProviderReviewRequestStatus.Submitted ||
                 r.Status == ScholarshipProviderReviewRequestStatus.Pending ||
                 r.Status == ScholarshipProviderReviewRequestStatus.UnderReview), ct);

        if (existing is { Status: ScholarshipProviderReviewRequestStatus.Submitted, Payment.StripePaymentIntentId: { } pid })
        {
            logger.LogInformation(
                "Re-using existing in-flight ScholarshipProviderReviewRequest {Id} for student {Student}.",
                existing.Id, studentId);
            return new StartScholarshipProviderReviewRequestResult(
                existing.Id,
                IsFree: false,
                existing.Payment!.Id,
                $"cs_existing_{pid}",
                pid,
                existing.Payment.AmountCents,
                existing.Payment.Currency);
        }

        // Free request idempotency: a previous free attempt already in Pending
        // is the terminal "submitted" state for free — re-use it instead of
        // throwing, so the Apply Now button is naturally idempotent.
        if (existing is { Status: ScholarshipProviderReviewRequestStatus.Pending, PaymentId: null })
        {
            logger.LogInformation(
                "Re-using existing free ScholarshipProviderReviewRequest {Id} for student {Student}.",
                existing.Id, studentId);
            return new StartScholarshipProviderReviewRequestResult(
                existing.Id,
                IsFree: true,
                PaymentId: null,
                ClientSecret: null,
                PaymentIntentId: null,
                AmountCents: 0,
                Currency: existing.Currency);
        }

        if (existing is not null)
            throw new ConflictException(
                "You already have an active review request for this scholarship.");

        // ── Free path: no Stripe, jump straight to Pending ──────────────────
        if (isFree)
        {
            var freeRequestId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var freeEntity = new ScholarshipProviderReviewRequest
            {
                Id = freeRequestId,
                StudentId = studentId,
                ScholarshipProviderId = companyId,
                ScholarshipId = scholarship.Id,
                ApplicationTrackerId = request.ApplicationTrackerId,
                PaymentId = null,
                Status = ScholarshipProviderReviewRequestStatus.Pending,
                ReviewFeeUsdSnapshot = 0m,
                Currency = currency.ToUpperInvariant(),
                SubmittedAt = now,
                PendingExpiresAt = now.AddDays(PendingTtlDays),
            };

            db.ScholarshipProviderReviewRequests.Add(freeEntity);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Created FREE ScholarshipProviderReviewRequest {RequestId} student={Student} scholarship={Scholarship}.",
                freeEntity.Id, studentId, scholarship.Id);

            return new StartScholarshipProviderReviewRequestResult(
                freeEntity.Id,
                IsFree: true,
                PaymentId: null,
                ClientSecret: null,
                PaymentIntentId: null,
                AmountCents: 0,
                Currency: freeEntity.Currency);
        }

        // ── Create Stripe PaymentIntent in MANUAL-capture mode ──────────────
        // ScholarshipProviderReview is authorize-then-capture-on-accept (spec PART 4).
        // Note: the generic CreatePaymentIntentCommand defaults ScholarshipProviderReview
        // to "automatic" capture for the legacy single-shot review fee. The
        // PB-005 paid-support flow ALWAYS holds first, so we call Stripe
        // directly with the manual capture method and create the Payment row
        // ourselves — keeping the legacy command untouched.
        var requestId = Guid.NewGuid();
        var idempotencyKey = $"crr-start:{studentId:N}:{request.ScholarshipId:N}";

        var split = await FinancialRuleResolver
            .ResolvePaymentSplitAsync(db, PaymentType.ScholarshipProviderReview, amountCents, ct);

        var stripeResult = await stripe.CreatePaymentIntentAsync(
            amountCents: amountCents,
            // Stripe wants the ISO-4217 currency in lower-case; CA1308's
            // "prefer ToUpperInvariant" rule is about normalisation for
            // comparison, not for serialising over the wire. Match the
            // existing CreatePaymentIntentCommand pattern.
#pragma warning disable CA1308
            currency: currency.ToLowerInvariant(),
#pragma warning restore CA1308
            captureMethod: "manual",
            metadata: new Dictionary<string, string>
            {
                ["company_review_request_id"] = requestId.ToString(),
                ["scholarship_id"] = scholarship.Id.ToString(),
                ["student_id"] = studentId.ToString(),
                ["company_id"] = companyId.ToString(),
            },
            idempotencyKey: idempotencyKey,
            ct: ct);

        if (stripeResult.ClientSecret is null)
            throw new ConflictException(
                $"Stripe did not return a ClientSecret. Status: {stripeResult.Status}");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ScholarshipProviderReview,
            Status = PaymentStatus.Pending,
            AmountCents = amountCents,
            Currency = currency.ToUpperInvariant(),
            ProfitShareAmountCents = split.PlatformTakeCents,
            PayeeAmountCents = split.PayeeNetCents,
            RefundedAmountCents = 0,
            PayerUserId = studentId,
            PayeeUserId = companyId,
            StripePaymentIntentId = stripeResult.Id,
            StripeChargeId = stripeResult.LatestChargeId,
            IdempotencyKey = idempotencyKey,
            RelatedApplicationId = request.ApplicationTrackerId,
        };

        var entity = new ScholarshipProviderReviewRequest
        {
            Id = requestId,
            StudentId = studentId,
            ScholarshipProviderId = companyId,
            ScholarshipId = scholarship.Id,
            ApplicationTrackerId = request.ApplicationTrackerId,
            PaymentId = payment.Id,
            Status = ScholarshipProviderReviewRequestStatus.Submitted,
            ReviewFeeUsdSnapshot = fee,
            Currency = currency.ToUpperInvariant(),
            SubmittedAt = DateTimeOffset.UtcNow,
            // Pending TTL is enforced by the Expire command; the column lets
            // dashboards show the deadline and the sweep job filter cheaply.
            PendingExpiresAt = DateTimeOffset.UtcNow.AddDays(PendingTtlDays),
        };

        db.Payments.Add(payment);
        db.ScholarshipProviderReviewRequests.Add(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created ScholarshipProviderReviewRequest {RequestId} payment={PaymentId} amount={Amount}c intent={Intent}",
            entity.Id, payment.Id, payment.AmountCents, stripeResult.Id);

        return new StartScholarshipProviderReviewRequestResult(
            entity.Id,
            IsFree: false,
            payment.Id,
            stripeResult.ClientSecret,
            stripeResult.Id,
            payment.AmountCents,
            payment.Currency);
    }
}
