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
using ScholarPath.Application.Payments;

namespace ScholarPath.Application.CompanyReviewRequests.Commands.Expire;

/// <summary>
/// Marks a Pending CompanyReviewRequest as Expired (the Company didn't respond
/// before <see cref="Domain.Entities.CompanyReviewRequest.PendingExpiresAt"/>).
/// Cancels the held PaymentIntent — no charge. Designed to be invoked from a
/// background sweep job; admin-only when called interactively.
/// </summary>
[Auditable(AuditAction.Update, "CompanyReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Expired CompanyReviewRequest {RequestId}")]
public sealed record ExpireCompanyReviewRequestCommand(
    Guid RequestId,
    bool SkipOwnerCheck = false) : IRequest<bool>;

public sealed class ExpireCompanyReviewRequestCommandValidator
    : AbstractValidator<ExpireCompanyReviewRequestCommand>
{
    public ExpireCompanyReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class ExpireCompanyReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<ExpireCompanyReviewRequestCommandHandler> logger)
    : IRequestHandler<ExpireCompanyReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        ExpireCompanyReviewRequestCommand command,
        CancellationToken ct)
    {
        if (!command.SkipOwnerCheck && !currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only an administrator can manually expire a request.");

        var entity = await db.CompanyReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyReviewRequest), command.RequestId);

        if (entity.Status == CompanyReviewRequestStatus.Expired)
            return false;

        if (entity.Status != CompanyReviewRequestStatus.Pending)
            throw new ConflictException(
                $"Cannot expire a CompanyReviewRequest in status {entity.Status} — only Pending requests expire.");

        if (entity.Payment is not null && entity.Payment.StripePaymentIntentId is not null
            && entity.Payment.Status == PaymentStatus.Held)
        {
            await stripe.CancelHeldPaymentAsync(
                entity.Payment.StripePaymentIntentId,
                idempotencyKey: $"crr-expire:{entity.Id:N}",
                ct);

            entity.Payment.Status = PaymentStatus.Cancelled;
            entity.Payment.RefundedAt = DateTimeOffset.UtcNow;
            entity.Payment.RefundReason = "Expired — Company did not respond in time; hold released";
        }

        entity.Status = CompanyReviewRequestStatus.Expired;
        entity.ExpiredAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

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
            NotificationType.CompanyReviewRequestPaymentHoldCancelled,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-expired-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.CompanyId,
            NotificationType.CompanyReviewRequestPaymentHoldCancelled,
            paramsForCompany,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-expired-company:{entity.Id:N}",
            ct);

        logger.LogInformation(
            "CompanyReviewRequest {RequestId} expired (hold released, no charge).", entity.Id);

        return true;
    }
}
