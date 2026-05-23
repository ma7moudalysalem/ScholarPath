using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.CompanyReviewRequests.Commands.Start;

// ─── Result ───────────────────────────────────────────────────────────────────

/// <summary>
/// Returned to the Student's Apply Now click. The frontend uses
/// <see cref="ClientSecret"/> with Stripe Elements to authorise the card
/// (manual capture) and then calls
/// <see cref="ScholarPath.Application.CompanyReviewRequests.Commands.ConfirmHold.ConfirmCompanyReviewRequestHoldCommand"/>
/// to flip the request from Submitted to Pending.
/// </summary>
public sealed record StartCompanyReviewRequestResult(
    Guid RequestId,
    Guid PaymentId,
    string ClientSecret,
    string PaymentIntentId,
    long AmountCents,
    string Currency);

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Student-initiated Apply Now (PB-005 final business rule). The Student pays
/// the Company for application-support / document-review; ScholarPath takes
/// 10% commission from the final retained captured payment, Company receives
/// 90% — calculated at capture (and re-calculated on partial refund) via
/// <see cref="FinancialRuleResolver"/>.
/// </summary>
[Auditable(AuditAction.Create, "CompanyReviewRequest",
    SummaryTemplate = "Started CompanyReview request for scholarship {ScholarshipId}")]
public sealed record StartCompanyReviewRequestCommand(
    Guid ScholarshipId,
    Guid? ApplicationTrackerId = null) : IRequest<StartCompanyReviewRequestResult>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class StartCompanyReviewRequestCommandValidator
    : AbstractValidator<StartCompanyReviewRequestCommand>
{
    public StartCompanyReviewRequestCommandValidator()
    {
        RuleFor(x => x.ScholarshipId)
            .NotEmpty()
            .WithMessage("ScholarshipId is required.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class StartCompanyReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    ILogger<StartCompanyReviewRequestCommandHandler> logger)
    : IRequestHandler<StartCompanyReviewRequestCommand, StartCompanyReviewRequestResult>
{
    private const int PendingTtlDays = 7;

    public async Task<StartCompanyReviewRequestResult> Handle(
        StartCompanyReviewRequestCommand request,
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

        if (scholarship.OwnerCompanyId is null)
            throw new ConflictException("This scholarship has no Company owner — Apply Now is not available.");

        if (scholarship.OwnerCompanyId == studentId)
            throw new ConflictException("A Company cannot start a paid review request for its own scholarship.");

        if (scholarship.ReviewFeeUsd is not { } fee || fee <= 0m)
            throw new ConflictException(
                "Review Service Fee is not configured for this scholarship. Apply Now is unavailable.");

        var companyId = scholarship.OwnerCompanyId.Value;
        var currency = scholarship.Currency ?? "USD";
        var amountCents = (long)Math.Round(fee * 100m, MidpointRounding.AwayFromZero);

        // ── Idempotency: re-use an in-flight request for the same student/scholarship ─
        var existing = await db.CompanyReviewRequests
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r =>
                r.StudentId == studentId &&
                r.ScholarshipId == request.ScholarshipId &&
                (r.Status == CompanyReviewRequestStatus.Draft ||
                 r.Status == CompanyReviewRequestStatus.Submitted ||
                 r.Status == CompanyReviewRequestStatus.Pending ||
                 r.Status == CompanyReviewRequestStatus.UnderReview), ct);

        if (existing is { Status: CompanyReviewRequestStatus.Submitted, Payment.StripePaymentIntentId: { } pid })
        {
            logger.LogInformation(
                "Re-using existing in-flight CompanyReviewRequest {Id} for student {Student}.",
                existing.Id, studentId);
            return new StartCompanyReviewRequestResult(
                existing.Id,
                existing.Payment!.Id,
                $"cs_existing_{pid}",
                pid,
                existing.Payment.AmountCents,
                existing.Payment.Currency);
        }

        if (existing is not null)
            throw new ConflictException(
                "You already have an active review request for this scholarship.");

        // ── Create Stripe PaymentIntent in MANUAL-capture mode ──────────────
        // CompanyReview is authorize-then-capture-on-accept (spec PART 4).
        // Note: the generic CreatePaymentIntentCommand defaults CompanyReview
        // to "automatic" capture for the legacy single-shot review fee. The
        // PB-005 paid-support flow ALWAYS holds first, so we call Stripe
        // directly with the manual capture method and create the Payment row
        // ourselves — keeping the legacy command untouched.
        var requestId = Guid.NewGuid();
        var idempotencyKey = $"crr-start:{studentId:N}:{request.ScholarshipId:N}";

        var split = await FinancialRuleResolver
            .ResolvePaymentSplitAsync(db, PaymentType.CompanyReview, amountCents, ct);

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
            Type = PaymentType.CompanyReview,
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

        var entity = new CompanyReviewRequest
        {
            Id = requestId,
            StudentId = studentId,
            CompanyId = companyId,
            ScholarshipId = scholarship.Id,
            ApplicationTrackerId = request.ApplicationTrackerId,
            PaymentId = payment.Id,
            Status = CompanyReviewRequestStatus.Submitted,
            ReviewFeeUsdSnapshot = fee,
            Currency = currency.ToUpperInvariant(),
            SubmittedAt = DateTimeOffset.UtcNow,
            // Pending TTL is enforced by the Expire command; the column lets
            // dashboards show the deadline and the sweep job filter cheaply.
            PendingExpiresAt = DateTimeOffset.UtcNow.AddDays(PendingTtlDays),
        };

        db.Payments.Add(payment);
        db.CompanyReviewRequests.Add(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created CompanyReviewRequest {RequestId} payment={PaymentId} amount={Amount}c intent={Intent}",
            entity.Id, payment.Id, payment.AmountCents, stripeResult.Id);

        return new StartCompanyReviewRequestResult(
            entity.Id,
            payment.Id,
            stripeResult.ClientSecret,
            stripeResult.Id,
            payment.AmountCents,
            payment.Currency);
    }
}
