using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Common;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Application.Payments;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Cancel;

/// <summary>
/// Student-initiated cancellation of a ScholarshipProviderReviewRequest. The refund policy
/// depends on the request's current status (spec PART 5):
///   • Submitted: cancel the (pending) intent — no charge → Cancelled.
///   • Pending:   cancel the hold — no charge → CancelledByStudent.
///   • UnderReview: 50% refund — retain 50% → CancelledByStudent.
///   • Completed / Closed / already-terminal: rejected.
///
/// Commission and ScholarshipProvider share are re-locked from the RETAINED amount at
/// refund time, so the 10/90 split always reflects the money the platform
/// actually keeps. Disputed and ScholarshipProviderFailure flows are deliberately not
/// supported here.
/// </summary>
[Auditable(AuditAction.Update, "ScholarshipProviderReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Student cancelled ScholarshipProviderReviewRequest {RequestId}")]
public sealed record CancelScholarshipProviderReviewRequestCommand(
    Guid RequestId,
    string? Reason = null) : IRequest<bool>;

public sealed class CancelScholarshipProviderReviewRequestCommandValidator
    : AbstractValidator<CancelScholarshipProviderReviewRequestCommand>
{
    public CancelScholarshipProviderReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class CancelScholarshipProviderReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<CancelScholarshipProviderReviewRequestCommandHandler> logger)
    : IRequestHandler<CancelScholarshipProviderReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        CancelScholarshipProviderReviewRequestCommand command,
        CancellationToken ct)
    {
        var entity = await db.ScholarshipProviderReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.ScholarshipProvider)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ScholarshipProviderReviewRequest), command.RequestId);

        if (entity.StudentId != currentUser.UserId)
            throw new ForbiddenAccessException();

        switch (entity.Status)
        {
            case ScholarshipProviderReviewRequestStatus.Submitted:
                await CancelSubmittedAsync(entity, command.Reason, ct);
                break;

            case ScholarshipProviderReviewRequestStatus.Pending:
                await CancelPendingAsync(entity, command.Reason, ct);
                break;

            case ScholarshipProviderReviewRequestStatus.UnderReview:
                await CancelUnderReviewWithHalfRefundAsync(entity, command.Reason, ct);
                break;

            case ScholarshipProviderReviewRequestStatus.Cancelled:
            case ScholarshipProviderReviewRequestStatus.CancelledByStudent:
                // Idempotent — already cancelled.
                return false;

            case ScholarshipProviderReviewRequestStatus.Completed:
            case ScholarshipProviderReviewRequestStatus.Closed:
                throw new ConflictException(
                    "This request is already completed. Cancellation and refund are no longer available.");

            default:
                throw new ConflictException(
                    $"Cannot cancel a ScholarshipProviderReviewRequest in status {entity.Status}.");
        }

        await db.SaveChangesAsync(ct);
        await DispatchAsync(entity, ct);
        return true;
    }

    private async Task CancelSubmittedAsync(
        Domain.Entities.ScholarshipProviderReviewRequest entity, string? reason, CancellationToken ct)
    {
        // Submitted means the PaymentIntent exists but the card hasn't been
        // authorised yet. Cancel the intent so Stripe releases any reserved
        // capacity; the Payment row goes to Cancelled and the request to the
        // generic Cancelled state (spec: "Submitted → Cancelled").
        if (entity.Payment is not null && entity.Payment.StripePaymentIntentId is not null
            && entity.Payment.Status == PaymentStatus.Pending)
        {
            await stripe.CancelHeldPaymentAsync(
                entity.Payment.StripePaymentIntentId,
                idempotencyKey: $"crr-cancel:{entity.Id:N}",
                ct);

            entity.Payment.Status = PaymentStatus.Cancelled;
            entity.Payment.RefundedAt = DateTimeOffset.UtcNow;
            entity.Payment.RefundReason = reason ?? "Cancelled by Student before payment authorisation";
        }

        entity.Status = ScholarshipProviderReviewRequestStatus.Cancelled;
        entity.CancelledAt = DateTimeOffset.UtcNow;
        entity.CancelReason = reason;

        logger.LogInformation(
            "ScholarshipProviderReviewRequest {RequestId} cancelled from Submitted by Student (no charge).",
            entity.Id);
    }

    private async Task CancelPendingAsync(
        Domain.Entities.ScholarshipProviderReviewRequest entity, string? reason, CancellationToken ct)
    {
        if (entity.Payment is not null && entity.Payment.StripePaymentIntentId is not null
            && entity.Payment.Status == PaymentStatus.Held)
        {
            await stripe.CancelHeldPaymentAsync(
                entity.Payment.StripePaymentIntentId,
                idempotencyKey: $"crr-cancel:{entity.Id:N}",
                ct);

            entity.Payment.Status = PaymentStatus.Cancelled;
            entity.Payment.RefundedAt = DateTimeOffset.UtcNow;
            entity.Payment.RefundReason = reason ?? "Cancelled by Student before ScholarshipProvider acceptance";
        }

        entity.Status = ScholarshipProviderReviewRequestStatus.CancelledByStudent;
        entity.CancelledAt = DateTimeOffset.UtcNow;
        entity.CancelReason = reason;

        logger.LogInformation(
            "ScholarshipProviderReviewRequest {RequestId} cancelled from Pending by Student (hold released, no charge).",
            entity.Id);
    }

    private async Task CancelUnderReviewWithHalfRefundAsync(
        Domain.Entities.ScholarshipProviderReviewRequest entity, string? reason, CancellationToken ct)
    {
        // Free request (no Payment row): the half-refund step is a no-op, but
        // the state still has to flip so the ScholarshipProvider stops working on it.
        if (entity.PaymentId is null)
        {
            entity.Status = ScholarshipProviderReviewRequestStatus.CancelledByStudent;
            entity.CancelledAt = DateTimeOffset.UtcNow;
            entity.CancelReason = reason;

            logger.LogInformation(
                "ScholarshipProviderReviewRequest {RequestId} cancelled from UnderReview by Student (FREE — no refund).",
                entity.Id);
            return;
        }

        if (entity.Payment is null || entity.Payment.StripePaymentIntentId is null)
            throw new ConflictException("ScholarshipProviderReviewRequest has no payment to refund.");
        if (entity.Payment.Status != PaymentStatus.Captured)
            throw new ConflictException(
                $"Cannot refund a payment in status {entity.Payment.Status} — expected Captured.");

        // 50% refund of the original gross. Integer cents with half-up rounding
        // so the refunded and retained halves re-sum exactly to the gross even
        // for odd-cent amounts (e.g. 999c → refund 500c, retain 499c).
        var refundCents = (long)Math.Round(entity.Payment.AmountCents / 2m, MidpointRounding.AwayFromZero);

        var refundResult = await stripe.RefundPaymentAsync(
            paymentIntentId: entity.Payment.StripePaymentIntentId,
            amountCents: refundCents,
            reason: "requested_by_customer",
            idempotencyKey: $"crr-refund:{entity.Id:N}",
            ct: ct);

        if (refundResult.Status != "succeeded")
            throw new ConflictException(
                $"Stripe refund did not succeed. Status: {refundResult.Status}");

        entity.Payment.RefundedAmountCents += refundResult.AmountCents;
        entity.Payment.RefundedAt = DateTimeOffset.UtcNow;
        entity.Payment.RefundReason = reason ?? "Cancelled by Student during UnderReview — 50% refund";
        entity.Payment.Status = PaymentStatus.PartiallyRefunded;

        // Re-lock the 10/90 split from the RETAINED amount so platform commission
        // and ScholarshipProvider share both reflect what the platform actually keeps. The
        // resolver returns the same 10% default for ScholarshipProviderReview unless a rule
        // overrides it (spec PART 6).
        var retainedCents = Math.Max(0, entity.Payment.AmountCents - entity.Payment.RefundedAmountCents);
        var split = await FinancialRuleResolver
            .ResolvePaymentSplitAsync(db, PaymentType.ScholarshipProviderReview, retainedCents, ct);
        entity.Payment.ProfitShareAmountCents = split.PlatformTakeCents;
        entity.Payment.PayeeAmountCents = split.PayeeNetCents;

        entity.Status = ScholarshipProviderReviewRequestStatus.CancelledByStudent;
        entity.CancelledAt = DateTimeOffset.UtcNow;
        entity.CancelReason = reason;

        logger.LogInformation(
            "ScholarshipProviderReviewRequest {RequestId} cancelled from UnderReview by Student — refunded {Refund}c, retained {Retained}c, commission={Commission}c, share={Share}c",
            entity.Id, refundCents, retainedCents,
            entity.Payment.ProfitShareAmountCents, entity.Payment.PayeeAmountCents);
    }

    private async Task DispatchAsync(
        Domain.Entities.ScholarshipProviderReviewRequest entity,
        CancellationToken ct)
    {
        var notifType = entity.Payment?.Status == PaymentStatus.PartiallyRefunded
            ? NotificationType.ScholarshipProviderReviewRequestPartiallyRefunded
            : NotificationType.ScholarshipProviderReviewRequestPaymentHoldCancelled;

        var paramsForStudent = ScholarshipProviderReviewRequestNotificationFactory.Build(
            entity, entity.Payment,
            entity.Scholarship?.TitleEn, entity.Scholarship?.TitleAr,
            counterpartyName: entity.ScholarshipProvider is null
                ? null
                : ($"{entity.ScholarshipProvider.FirstName} {entity.ScholarshipProvider.LastName}".Trim()));

        var paramsForScholarshipProvider = ScholarshipProviderReviewRequestNotificationFactory.Build(
            entity, entity.Payment,
            entity.Scholarship?.TitleEn, entity.Scholarship?.TitleAr,
            counterpartyName: entity.Student is null
                ? null
                : ($"{entity.Student.FirstName} {entity.Student.LastName}".Trim()));

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.StudentId, notifType, paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-cancel-student:{entity.Id:N}:{(int)entity.Status}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.ScholarshipProviderId, notifType, paramsForScholarshipProvider,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-cancel-company:{entity.Id:N}:{(int)entity.Status}",
            ct);
    }
}
