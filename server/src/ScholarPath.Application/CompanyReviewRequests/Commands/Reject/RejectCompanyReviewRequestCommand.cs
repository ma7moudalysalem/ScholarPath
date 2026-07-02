using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Common;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Application.Payments;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Reject;

/// <summary>
/// ScholarshipProvider-side reject of a Pending ScholarshipProviderReviewRequest. Cancels the held
/// Stripe PaymentIntent (no charge), flips the request to RejectedByScholarshipProvider,
/// and notifies the Student.
/// </summary>
[Auditable(AuditAction.Rejected, "ScholarshipProviderReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "ScholarshipProvider rejected ScholarshipProviderReviewRequest {RequestId} — released hold")]
public sealed record RejectScholarshipProviderReviewRequestCommand(
    Guid RequestId,
    string? Reason = null) : IRequest<bool>;

public sealed class RejectScholarshipProviderReviewRequestCommandValidator
    : AbstractValidator<RejectScholarshipProviderReviewRequestCommand>
{
    public RejectScholarshipProviderReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class RejectScholarshipProviderReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<RejectScholarshipProviderReviewRequestCommandHandler> logger)
    : IRequestHandler<RejectScholarshipProviderReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        RejectScholarshipProviderReviewRequestCommand command,
        CancellationToken ct)
    {
        var entity = await db.ScholarshipProviderReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.ScholarshipProvider)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ScholarshipProviderReviewRequest), command.RequestId);

        if (entity.ScholarshipProviderId != currentUser.UserId)
            throw new ForbiddenAccessException();

        if (entity.Status == ScholarshipProviderReviewRequestStatus.RejectedByScholarshipProvider)
            return false;

        if (entity.Status != ScholarshipProviderReviewRequestStatus.Pending)
            throw new ConflictException(
                $"Cannot reject a ScholarshipProviderReviewRequest in status {entity.Status} — only Pending requests can be rejected by the ScholarshipProvider.");

        if (entity.Payment is not null && entity.Payment.StripePaymentIntentId is not null
            && entity.Payment.Status == PaymentStatus.Held)
        {
            await stripe.CancelHeldPaymentAsync(
                entity.Payment.StripePaymentIntentId,
                idempotencyKey: $"crr-reject:{entity.Id:N}",
                ct);

            entity.Payment.Status = PaymentStatus.Cancelled;
            entity.Payment.RefundedAt = DateTimeOffset.UtcNow;
            entity.Payment.RefundReason = command.Reason ?? "Rejected by ScholarshipProvider — hold released";
        }

        entity.Status = ScholarshipProviderReviewRequestStatus.RejectedByScholarshipProvider;
        entity.RejectedAt = DateTimeOffset.UtcNow;
        entity.RejectReason = command.Reason;

        await db.SaveChangesAsync(ct);

        await DispatchAsync(entity, ct);

        logger.LogInformation(
            "ScholarshipProviderReviewRequest {RequestId} rejected by ScholarshipProvider → RejectedByScholarshipProvider (hold released)",
            entity.Id);

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
            NotificationType.ScholarshipProviderReviewRequestPaymentHoldCancelled,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-rejected-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.ScholarshipProviderId,
            NotificationType.ScholarshipProviderReviewRequestPaymentHoldCancelled,
            paramsForScholarshipProvider,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-rejected-company:{entity.Id:N}",
            ct);
    }
}
