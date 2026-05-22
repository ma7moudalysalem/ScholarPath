using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Common;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.CompanyReviewRequests.Commands.ConfirmHold;

/// <summary>
/// Confirms that Stripe has successfully authorised the card hold for a
/// Submitted CompanyReviewRequest, flipping the request to Pending and the
/// Payment to Held. Dispatches the "payment held" notification to both
/// parties.
///
/// Designed for two callers:
///   1. The Student's Apply Now flow, after Stripe Elements resolves the
///      manual-capture confirmation.
///   2. The Stripe webhook handler (out of scope for this branch) — same
///      handler, called system-side; the role check is skipped via
///      <see cref="ConfirmCompanyReviewRequestHoldCommand.SkipOwnerCheck"/>.
///
/// Idempotent — re-confirming a Pending request is a no-op.
/// </summary>
[Auditable(AuditAction.Update, "CompanyReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Confirmed payment hold for CompanyReviewRequest {RequestId}")]
public sealed record ConfirmCompanyReviewRequestHoldCommand(
    Guid RequestId,
    bool SkipOwnerCheck = false) : IRequest<bool>;

public sealed class ConfirmCompanyReviewRequestHoldCommandValidator
    : AbstractValidator<ConfirmCompanyReviewRequestHoldCommand>
{
    public ConfirmCompanyReviewRequestHoldCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class ConfirmCompanyReviewRequestHoldCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<ConfirmCompanyReviewRequestHoldCommandHandler> logger)
    : IRequestHandler<ConfirmCompanyReviewRequestHoldCommand, bool>
{
    public async Task<bool> Handle(
        ConfirmCompanyReviewRequestHoldCommand command,
        CancellationToken ct)
    {
        var entity = await db.CompanyReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyReviewRequest), command.RequestId);

        if (!command.SkipOwnerCheck && entity.StudentId != currentUser.UserId)
            throw new ForbiddenAccessException();

        // Idempotent: already Pending or further along.
        if (entity.Status == CompanyReviewRequestStatus.Pending ||
            entity.Status == CompanyReviewRequestStatus.UnderReview ||
            entity.Status == CompanyReviewRequestStatus.Completed ||
            entity.Status == CompanyReviewRequestStatus.Closed)
        {
            return false;
        }

        if (entity.Status != CompanyReviewRequestStatus.Submitted)
            throw new ConflictException(
                $"Cannot confirm hold from status {entity.Status} — only Submitted requests can move to Pending.");

        if (entity.Payment is null)
            throw new ConflictException(
                "CompanyReviewRequest has no associated Payment row.");

        entity.Payment.Status = PaymentStatus.Held;
        entity.Payment.HeldAt = DateTimeOffset.UtcNow;

        entity.Status = CompanyReviewRequestStatus.Pending;

        await db.SaveChangesAsync(ct);

        await DispatchHeldNotificationsAsync(entity, ct);

        logger.LogInformation(
            "CompanyReviewRequest {RequestId} confirmed Held → Pending (payment={PaymentId})",
            entity.Id, entity.Payment.Id);

        return true;
    }

    private async Task DispatchHeldNotificationsAsync(
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
            NotificationType.CompanyReviewRequestPaymentHeld,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-held-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.CompanyId,
            NotificationType.CompanyReviewRequestIncoming,
            paramsForCompany,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-held-company:{entity.Id:N}",
            ct);
    }
}
