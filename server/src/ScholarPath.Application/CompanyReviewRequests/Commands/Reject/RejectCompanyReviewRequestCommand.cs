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

namespace ScholarPath.Application.CompanyReviewRequests.Commands.Reject;

/// <summary>
/// Company-side reject of a Pending CompanyReviewRequest. Cancels the held
/// Stripe PaymentIntent (no charge), flips the request to RejectedByCompany,
/// and notifies the Student.
/// </summary>
[Auditable(AuditAction.Rejected, "CompanyReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Company rejected CompanyReviewRequest {RequestId} — released hold")]
public sealed record RejectCompanyReviewRequestCommand(
    Guid RequestId,
    string? Reason = null) : IRequest<bool>;

public sealed class RejectCompanyReviewRequestCommandValidator
    : AbstractValidator<RejectCompanyReviewRequestCommand>
{
    public RejectCompanyReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class RejectCompanyReviewRequestCommandHandler(
    IApplicationDbContext db,
    IStripeService stripe,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<RejectCompanyReviewRequestCommandHandler> logger)
    : IRequestHandler<RejectCompanyReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        RejectCompanyReviewRequestCommand command,
        CancellationToken ct)
    {
        var entity = await db.CompanyReviewRequests
            .Include(r => r.Payment)
            .Include(r => r.Scholarship)
            .Include(r => r.Student)
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyReviewRequest), command.RequestId);

        if (entity.CompanyId != currentUser.UserId)
            throw new ForbiddenAccessException();

        if (entity.Status == CompanyReviewRequestStatus.RejectedByCompany)
            return false;

        if (entity.Status != CompanyReviewRequestStatus.Pending)
            throw new ConflictException(
                $"Cannot reject a CompanyReviewRequest in status {entity.Status} — only Pending requests can be rejected by the Company.");

        if (entity.Payment is not null && entity.Payment.StripePaymentIntentId is not null
            && entity.Payment.Status == PaymentStatus.Held)
        {
            await stripe.CancelHeldPaymentAsync(
                entity.Payment.StripePaymentIntentId,
                idempotencyKey: $"crr-reject:{entity.Id:N}",
                ct);

            entity.Payment.Status = PaymentStatus.Cancelled;
            entity.Payment.RefundedAt = DateTimeOffset.UtcNow;
            entity.Payment.RefundReason = command.Reason ?? "Rejected by Company — hold released";
        }

        entity.Status = CompanyReviewRequestStatus.RejectedByCompany;
        entity.RejectedAt = DateTimeOffset.UtcNow;
        entity.RejectReason = command.Reason;

        await db.SaveChangesAsync(ct);

        await DispatchAsync(entity, ct);

        logger.LogInformation(
            "CompanyReviewRequest {RequestId} rejected by Company → RejectedByCompany (hold released)",
            entity.Id);

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
            NotificationType.CompanyReviewRequestPaymentHoldCancelled,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-rejected-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.CompanyId,
            NotificationType.CompanyReviewRequestPaymentHoldCancelled,
            paramsForCompany,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-rejected-company:{entity.Id:N}",
            ct);
    }
}
