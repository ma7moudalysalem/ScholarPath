using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Common;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Application.Payments;

namespace ScholarPath.Application.CompanyReviewRequests.Commands.Cancel;

/// <summary>
/// Student-initiated cancellation of a CompanyReviewRequest. The refund policy
/// depends on the request's current status (spec PART 5):
///   • Submitted: cancel the (pending) intent — no charge → Cancelled.
///   • Pending:   cancel the hold — no charge → CancelledByStudent.
///   • UnderReview: 50% refund — retain 50% → CancelledByStudent.
///   • Completed / Closed / already-terminal: rejected.
///
/// Commission and Company share are re-locked from the RETAINED amount at
/// refund time, so the 10/90 split always reflects the money the platform
/// actually keeps. Disputed and CompanyFailure flows are deliberately not
/// supported here.
/// </summary>
[Auditable(AuditAction.Update, "CompanyReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Student cancelled CompanyReviewRequest {RequestId}")]
public sealed record CancelCompanyReviewRequestCommand(
    Guid RequestId,
    string? Reason = null) : IRequest<bool>;

public sealed class CancelCompanyReviewRequestCommandValidator
    : AbstractValidator<CancelCompanyReviewRequestCommand>
{
    public CancelCompanyReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class CancelCompanyReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<CancelCompanyReviewRequestCommandHandler> logger)
    : IRequestHandler<CancelCompanyReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        CancelCompanyReviewRequestCommand command,
        CancellationToken ct)
    {
        var entity = await db.CompanyReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyReviewRequest), command.RequestId);

        if (entity.StudentId != currentUser.UserId)
            throw new ForbiddenAccessException();

        switch (entity.Status)
        {
            case CompanyReviewRequestStatus.Submitted:
                await CancelSubmittedAsync(entity, command.Reason, ct);
                break;

            case CompanyReviewRequestStatus.Pending:
                await CancelPendingAsync(entity, command.Reason, ct);
                break;

            case CompanyReviewRequestStatus.UnderReview:
                await CancelUnderReviewWithHalfRefundAsync(entity, command.Reason, ct);
                break;

            case CompanyReviewRequestStatus.Cancelled:
            case CompanyReviewRequestStatus.CancelledByStudent:
                // Idempotent — already cancelled.
                return false;

            case CompanyReviewRequestStatus.Completed:
            case CompanyReviewRequestStatus.Closed:
                throw new ConflictException(
                    "This request is already completed. Cancellation and refund are no longer available.");

            default:
                throw new ConflictException(
                    $"Cannot cancel a CompanyReviewRequest in status {entity.Status}.");
        }

        await db.SaveChangesAsync(ct);
        await DispatchAsync(entity, ct);
        return true;
    }

    private async Task CancelSubmittedAsync(
        Domain.Entities.CompanyReviewRequest entity, string? reason, CancellationToken ct)
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

        entity.Status = CompanyReviewRequestStatus.Cancelled;
        entity.CancelledAt = DateTimeOffset.UtcNow;
        entity.CancelReason = reason;

        logger.LogInformation(
            "CompanyReviewRequest {RequestId} cancelled from Submitted by Student (no charge).",
            entity.Id);
    }

    private async Task CancelPendingAsync(
        Domain.Entities.CompanyReviewRequest entity, string? reason, CancellationToken ct)
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
            entity.Payment.RefundReason = reason ?? "Cancelled by Student before Company acceptance";
        }

        entity.Status = CompanyReviewRequestStatus.CancelledByStudent;
        entity.CancelledAt = DateTimeOffset.UtcNow;
        entity.CancelReason = reason;

        logger.LogInformation(
            "CompanyReviewRequest {RequestId} cancelled from Pending by Student (hold released, no charge).",
            entity.Id);
    }

    private async Task CancelUnderReviewWithHalfRefundAsync(
        Domain.Entities.CompanyReviewRequest entity, string? reason, CancellationToken ct)
    {
        if (entity.Payment is null || entity.Payment.StripePaymentIntentId is null)
            throw new ConflictException("CompanyReviewRequest has no payment to refund.");
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
        // and Company share both reflect what the platform actually keeps. The
        // resolver returns the same 10% default for CompanyReview unless a rule
        // overrides it (spec PART 6).
        var retainedCents = Math.Max(0, entity.Payment.AmountCents - entity.Payment.RefundedAmountCents);
        var split = await FinancialRuleResolver
            .ResolvePaymentSplitAsync(db, PaymentType.CompanyReview, retainedCents, ct);
        entity.Payment.ProfitShareAmountCents = split.PlatformTakeCents;
        entity.Payment.PayeeAmountCents = split.PayeeNetCents;

        entity.Status = CompanyReviewRequestStatus.CancelledByStudent;
        entity.CancelledAt = DateTimeOffset.UtcNow;
        entity.CancelReason = reason;

        logger.LogInformation(
            "CompanyReviewRequest {RequestId} cancelled from UnderReview by Student — refunded {Refund}c, retained {Retained}c, commission={Commission}c, share={Share}c",
            entity.Id, refundCents, retainedCents,
            entity.Payment.ProfitShareAmountCents, entity.Payment.PayeeAmountCents);
    }

    private async Task DispatchAsync(
        Domain.Entities.CompanyReviewRequest entity,
        CancellationToken ct)
    {
        var notifType = entity.Payment?.Status == PaymentStatus.PartiallyRefunded
            ? NotificationType.CompanyReviewRequestPartiallyRefunded
            : NotificationType.CompanyReviewRequestPaymentHoldCancelled;

        var paramsForStudent = CompanyReviewRequestNotificationFactory.Build(
            entity, entity.Payment,
            entity.Scholarship?.TitleEn, entity.Scholarship?.TitleAr,
            counterpartyName: entity.Company is null
                ? null
                : ($"{entity.Company.FirstName} {entity.Company.LastName}".Trim()));

        var paramsForCompany = CompanyReviewRequestNotificationFactory.Build(
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
            entity.CompanyId, notifType, paramsForCompany,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-cancel-company:{entity.Id:N}:{(int)entity.Status}",
            ct);
    }
}
