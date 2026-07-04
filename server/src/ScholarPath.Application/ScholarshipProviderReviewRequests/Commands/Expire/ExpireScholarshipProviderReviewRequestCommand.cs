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

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Expire;

/// <summary>
/// Marks a Pending ScholarshipProviderReviewRequest as Expired (the ScholarshipProvider didn't respond
/// before <see cref="Domain.Entities.ScholarshipProviderReviewRequest.PendingExpiresAt"/>).
/// Cancels the held PaymentIntent — no charge. Designed to be invoked from a
/// background sweep job; admin-only when called interactively.
/// </summary>
[Auditable(AuditAction.Update, "ScholarshipProviderReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Expired ScholarshipProviderReviewRequest {RequestId}")]
public sealed record ExpireScholarshipProviderReviewRequestCommand(
    Guid RequestId,
    bool SkipOwnerCheck = false) : IRequest<bool>;

public sealed class ExpireScholarshipProviderReviewRequestCommandValidator
    : AbstractValidator<ExpireScholarshipProviderReviewRequestCommand>
{
    public ExpireScholarshipProviderReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class ExpireScholarshipProviderReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<ExpireScholarshipProviderReviewRequestCommandHandler> logger)
    : IRequestHandler<ExpireScholarshipProviderReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        ExpireScholarshipProviderReviewRequestCommand command,
        CancellationToken ct)
    {
        if (!command.SkipOwnerCheck && !currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can manually expire a request.");

        var entity = await db.ScholarshipProviderReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.ScholarshipProvider)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ScholarshipProviderReviewRequest), command.RequestId);

        if (entity.Status == ScholarshipProviderReviewRequestStatus.Expired)
            return false;

        if (entity.Status != ScholarshipProviderReviewRequestStatus.Pending)
            throw new ConflictException(
                $"Cannot expire a ScholarshipProviderReviewRequest in status {entity.Status} — only Pending requests expire.");

        if (entity.Payment is not null && entity.Payment.StripePaymentIntentId is not null
            && entity.Payment.Status == PaymentStatus.Held)
        {
            await stripe.CancelHeldPaymentAsync(
                entity.Payment.StripePaymentIntentId,
                idempotencyKey: $"crr-expire:{entity.Id:N}",
                ct);

            entity.Payment.Status = PaymentStatus.Cancelled;
            entity.Payment.RefundedAt = DateTimeOffset.UtcNow;
            entity.Payment.RefundReason = "Expired — ScholarshipProvider did not respond in time; hold released";
        }

        entity.Status = ScholarshipProviderReviewRequestStatus.Expired;
        entity.ExpiredAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

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
            idempotencyKey: $"crr-expired-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.ScholarshipProviderId,
            NotificationType.ScholarshipProviderReviewRequestPaymentHoldCancelled,
            paramsForScholarshipProvider,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-expired-company:{entity.Id:N}",
            ct);

        logger.LogInformation(
            "ScholarshipProviderReviewRequest {RequestId} expired (hold released, no charge).", entity.Id);

        return true;
    }
}
