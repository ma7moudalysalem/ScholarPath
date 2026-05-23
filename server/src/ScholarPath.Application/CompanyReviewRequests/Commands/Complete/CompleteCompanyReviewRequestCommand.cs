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

namespace ScholarPath.Application.CompanyReviewRequests.Commands.Complete;

/// <summary>
/// Company-side mark-as-complete for a CompanyReviewRequest in UnderReview.
/// Captured payment is retained (no refund by default — spec PART 5). The
/// request moves UnderReview → Completed; the Closed state is reachable
/// admin-side and is intentionally not part of this command.
/// </summary>
[Auditable(AuditAction.Update, "CompanyReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "Company completed CompanyReviewRequest {RequestId}")]
public sealed record CompleteCompanyReviewRequestCommand(Guid RequestId) : IRequest<bool>;

public sealed class CompleteCompanyReviewRequestCommandValidator
    : AbstractValidator<CompleteCompanyReviewRequestCommand>
{
    public CompleteCompanyReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class CompleteCompanyReviewRequestCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<CompleteCompanyReviewRequestCommandHandler> logger)
    : IRequestHandler<CompleteCompanyReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        CompleteCompanyReviewRequestCommand command,
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

        if (entity.Status == CompanyReviewRequestStatus.Completed ||
            entity.Status == CompanyReviewRequestStatus.Closed)
        {
            return false;
        }

        if (entity.Status != CompanyReviewRequestStatus.UnderReview)
            throw new ConflictException(
                $"Cannot complete a CompanyReviewRequest in status {entity.Status} — only UnderReview requests can be completed.");

        entity.Status = CompanyReviewRequestStatus.Completed;
        entity.CompletedAt = DateTimeOffset.UtcNow;

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
            NotificationType.CompanyReviewRequestCompleted,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-completed-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.CompanyId,
            NotificationType.CompanyReviewRequestCompleted,
            paramsForCompany,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-completed-company:{entity.Id:N}",
            ct);

        logger.LogInformation(
            "CompanyReviewRequest {RequestId} marked Completed by Company.", entity.Id);

        return true;
    }
}
