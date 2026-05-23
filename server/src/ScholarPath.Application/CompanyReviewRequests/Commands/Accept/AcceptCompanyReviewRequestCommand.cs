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

namespace ScholarPath.Application.CompanyReviewRequests.Commands.Accept;

/// <summary>
/// Company-side accept of a Pending CompanyReviewRequest. Captures the held
/// Stripe PaymentIntent, locks in the 10/90 platform/Company split from the
/// rule in force at capture time (FR-163..176), flips the request to
/// UnderReview, and dispatches receipts to both parties.
/// </summary>
[Auditable(AuditAction.PaymentCaptured, "CompanyReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Company accepted CompanyReviewRequest {RequestId} — captured payment")]
public sealed record AcceptCompanyReviewRequestCommand(Guid RequestId) : IRequest<bool>;

public sealed class AcceptCompanyReviewRequestCommandValidator
    : AbstractValidator<AcceptCompanyReviewRequestCommand>
{
    public AcceptCompanyReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class AcceptCompanyReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<AcceptCompanyReviewRequestCommandHandler> logger)
    : IRequestHandler<AcceptCompanyReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        AcceptCompanyReviewRequestCommand command,
        CancellationToken ct)
    {
        var entity = await db.CompanyReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyReviewRequest), command.RequestId);

        // Authorization — owning Company only (admin override deliberately
        // omitted: an admin pressing accept would charge a Student on behalf
        // of a Company they don't own, which is a refund-class accident waiting
        // to happen).
        if (entity.CompanyId != currentUser.UserId)
            throw new ForbiddenAccessException();

        // Idempotent: already accepted further along.
        if (entity.Status == CompanyReviewRequestStatus.UnderReview ||
            entity.Status == CompanyReviewRequestStatus.Completed ||
            entity.Status == CompanyReviewRequestStatus.Closed)
        {
            return false;
        }

        if (entity.Status != CompanyReviewRequestStatus.Pending)
            throw new ConflictException(
                $"Cannot accept a CompanyReviewRequest in status {entity.Status} — only Pending requests can be accepted.");

        if (entity.Payment is null || entity.Payment.StripePaymentIntentId is null)
            throw new ConflictException("CompanyReviewRequest has no payment intent to capture.");

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
        // 10% commission / 90% Company share for CompanyReview unless an
        // active financial rule overrides it.
        var split = await FinancialRuleResolver
            .ResolvePaymentSplitAsync(db, PaymentType.CompanyReview, entity.Payment.AmountCents, ct);
        entity.Payment.ProfitShareAmountCents = split.PlatformTakeCents;
        entity.Payment.PayeeAmountCents = split.PayeeNetCents;

        entity.Status = CompanyReviewRequestStatus.UnderReview;
        entity.AcceptedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await DispatchAsync(entity, ct);

        logger.LogInformation(
            "CompanyReviewRequest {RequestId} accepted → UnderReview; captured {Amount}c, commission={Commission}c, share={Share}c",
            entity.Id, entity.Payment.AmountCents,
            entity.Payment.ProfitShareAmountCents, entity.Payment.PayeeAmountCents);

        return true;
    }

    private async Task DispatchAsync(
        Domain.Entities.CompanyReviewRequest entity,
        CancellationToken ct)
    {
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
            entity.StudentId,
            NotificationType.CompanyReviewRequestPaymentCaptured,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-captured-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.CompanyId,
            NotificationType.CompanyReviewRequestPaymentCaptured,
            paramsForCompany,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-captured-company:{entity.Id:N}",
            ct);
    }
}
