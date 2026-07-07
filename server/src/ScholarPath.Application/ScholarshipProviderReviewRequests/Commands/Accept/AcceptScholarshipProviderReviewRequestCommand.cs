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

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Accept;

/// <summary>
/// ScholarshipProvider-side accept of a Pending ScholarshipProviderReviewRequest. Captures the held
/// Stripe PaymentIntent, locks in the 10/90 platform/ScholarshipProvider split from the
/// rule in force at capture time (FR-163..176), flips the request to
/// UnderReview, and dispatches receipts to both parties.
/// </summary>
// Audit as a neutral "accepted" — NOT PaymentCaptured. The old static label
// claimed "captured payment" on every accept, so in free mode (payments off, fee
// 0, Stripe bypassed) the audit log still read "captured payment" even though no
// money moved. The real payment capture, when it happens, is recorded on the
// Payment side. This keeps the request-accept entry accurate in both modes.
[Auditable(AuditAction.Approved, "ScholarshipProviderReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "ScholarshipProvider accepted review request {RequestId}")]
public sealed record AcceptScholarshipProviderReviewRequestCommand(Guid RequestId) : IRequest<bool>;

public sealed class AcceptScholarshipProviderReviewRequestCommandValidator
    : AbstractValidator<AcceptScholarshipProviderReviewRequestCommand>
{
    public AcceptScholarshipProviderReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class AcceptScholarshipProviderReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<AcceptScholarshipProviderReviewRequestCommandHandler> logger)
    : IRequestHandler<AcceptScholarshipProviderReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        AcceptScholarshipProviderReviewRequestCommand command,
        CancellationToken ct)
    {
        var entity = await db.ScholarshipProviderReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.ScholarshipProvider)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ScholarshipProviderReviewRequest), command.RequestId);

        // Authorization — owning ScholarshipProvider only (admin override deliberately
        // omitted: an admin pressing accept would charge a Student on behalf
        // of a ScholarshipProvider they don't own, which is a refund-class accident waiting
        // to happen).
        if (entity.ScholarshipProviderId != currentUser.UserId)
            throw new ForbiddenAccessException();

        // Idempotent: already accepted further along.
        if (entity.Status == ScholarshipProviderReviewRequestStatus.UnderReview ||
            entity.Status == ScholarshipProviderReviewRequestStatus.Completed ||
            entity.Status == ScholarshipProviderReviewRequestStatus.Closed)
        {
            return false;
        }

        if (entity.Status != ScholarshipProviderReviewRequestStatus.Pending)
            throw new ConflictException(
                $"Cannot accept a ScholarshipProviderReviewRequest in status {entity.Status} — only Pending requests can be accepted.");

        // Free request (PaymentId is null when the scholarship was free at
        // start time): skip Stripe entirely and just flip the state. There is
        // no money to capture, no commission to compute.
        if (entity.PaymentId is null)
        {
            entity.Status = ScholarshipProviderReviewRequestStatus.UnderReview;
            entity.AcceptedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            await DispatchAsync(entity, ct);

            logger.LogInformation(
                "ScholarshipProviderReviewRequest {RequestId} accepted → UnderReview (FREE — no capture).",
                entity.Id);
            return true;
        }

        if (entity.Payment is null || entity.Payment.StripePaymentIntentId is null)
            throw new ConflictException("ScholarshipProviderReviewRequest has no payment intent to capture.");

        var stripeResult = await stripe.CapturePaymentIntentAsync(
            paymentIntentId: entity.Payment.StripePaymentIntentId,
            amountToCaptureCents: null, // capture full held amount
            idempotencyKey: $"crr-accept:{entity.Id:N}",
            ct: ct);

        if (stripeResult.Status != "succeeded")
            throw new ConflictException(
                $"Stripe capture did not succeed. Status: {stripeResult.Status}");

        // ── State transition ────────────────────────────────────────────────
        entity.Payment.Status = PaymentStatus.Captured;
        entity.Payment.CapturedAt = DateTimeOffset.UtcNow;
        if (stripeResult.LatestChargeId is not null)
            entity.Payment.StripeChargeId = stripeResult.LatestChargeId;

        // Re-resolve the split at capture time so the locked-in numbers
        // reflect the rule active right now (FR-163..176). The default is
        // 10% commission / 90% ScholarshipProvider share for ScholarshipProviderReview unless an
        // active financial rule overrides it.
        var split = await FinancialRuleResolver
            .ResolvePaymentSplitAsync(db, PaymentType.ScholarshipProviderReview, entity.Payment.AmountCents, ct);
        entity.Payment.ProfitShareAmountCents = split.PlatformTakeCents;
        entity.Payment.PayeeAmountCents = split.PayeeNetCents;

        entity.Status = ScholarshipProviderReviewRequestStatus.UnderReview;
        entity.AcceptedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await DispatchAsync(entity, ct);

        logger.LogInformation(
            "ScholarshipProviderReviewRequest {RequestId} accepted → UnderReview; captured {Amount}c, commission={Commission}c, share={Share}c",
            entity.Id, entity.Payment.AmountCents,
            entity.Payment.ProfitShareAmountCents, entity.Payment.PayeeAmountCents);

        return true;
    }

    private async Task DispatchAsync(
        Domain.Entities.ScholarshipProviderReviewRequest entity,
        CancellationToken ct)
    {
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
            entity.StudentId,
            NotificationType.ScholarshipProviderReviewRequestPaymentCaptured,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-captured-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.ScholarshipProviderId,
            NotificationType.ScholarshipProviderReviewRequestPaymentCaptured,
            paramsForScholarshipProvider,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-captured-company:{entity.Id:N}",
            ct);
    }
}
