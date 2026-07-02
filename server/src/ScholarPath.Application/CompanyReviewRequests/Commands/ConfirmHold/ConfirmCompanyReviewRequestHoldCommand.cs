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

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.ConfirmHold;

/// <summary>
/// Confirms that Stripe has successfully authorised the card hold for a
/// Submitted ScholarshipProviderReviewRequest, flipping the request to Pending and the
/// Payment to Held. Dispatches the "payment held" notification to both
/// parties.
///
/// Designed for two callers:
///   1. The Student's Apply Now flow, after Stripe Elements resolves the
///      manual-capture confirmation.
///   2. The Stripe webhook handler (out of scope for this branch) — same
///      handler, called system-side; the role check is skipped via
///      <see cref="ConfirmScholarshipProviderReviewRequestHoldCommand.SkipOwnerCheck"/>.
///
/// Idempotent — re-confirming a Pending request is a no-op.
/// </summary>
[Auditable(AuditAction.Update, "ScholarshipProviderReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Confirmed payment hold for ScholarshipProviderReviewRequest {RequestId}")]
public sealed record ConfirmScholarshipProviderReviewRequestHoldCommand(
    Guid RequestId,
    bool SkipOwnerCheck = false) : IRequest<bool>;

public sealed class ConfirmScholarshipProviderReviewRequestHoldCommandValidator
    : AbstractValidator<ConfirmScholarshipProviderReviewRequestHoldCommand>
{
    public ConfirmScholarshipProviderReviewRequestHoldCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class ConfirmScholarshipProviderReviewRequestHoldCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<ConfirmScholarshipProviderReviewRequestHoldCommandHandler> logger)
    : IRequestHandler<ConfirmScholarshipProviderReviewRequestHoldCommand, bool>
{
    public async Task<bool> Handle(
        ConfirmScholarshipProviderReviewRequestHoldCommand command,
        CancellationToken ct)
    {
        var entity = await db.ScholarshipProviderReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.ScholarshipProvider)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ScholarshipProviderReviewRequest), command.RequestId);

        if (!command.SkipOwnerCheck && entity.StudentId != currentUser.UserId)
            throw new ForbiddenAccessException();

        // Idempotent: already Pending or further along.
        if (entity.Status == ScholarshipProviderReviewRequestStatus.Pending ||
            entity.Status == ScholarshipProviderReviewRequestStatus.UnderReview ||
            entity.Status == ScholarshipProviderReviewRequestStatus.Completed ||
            entity.Status == ScholarshipProviderReviewRequestStatus.Closed)
        {
            return false;
        }

        if (entity.Status != ScholarshipProviderReviewRequestStatus.Submitted)
            throw new ConflictException(
                $"Cannot confirm hold from status {entity.Status} — only Submitted requests can move to Pending.");

        if (entity.Payment is null)
            throw new ConflictException(
                "ScholarshipProviderReviewRequest has no associated Payment row.");

        entity.Payment.Status = PaymentStatus.Held;
        entity.Payment.HeldAt = DateTimeOffset.UtcNow;

        entity.Status = ScholarshipProviderReviewRequestStatus.Pending;

        await db.SaveChangesAsync(ct);

        await DispatchHeldNotificationsAsync(entity, ct);

        logger.LogInformation(
            "ScholarshipProviderReviewRequest {RequestId} confirmed Held → Pending (payment={PaymentId})",
            entity.Id, entity.Payment.Id);

        return true;
    }

    private async Task DispatchHeldNotificationsAsync(
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
            NotificationType.ScholarshipProviderReviewRequestPaymentHeld,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-held-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.ScholarshipProviderId,
            NotificationType.ScholarshipProviderReviewRequestIncoming,
            paramsForScholarshipProvider,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-held-company:{entity.Id:N}",
            ct);
    }
}
