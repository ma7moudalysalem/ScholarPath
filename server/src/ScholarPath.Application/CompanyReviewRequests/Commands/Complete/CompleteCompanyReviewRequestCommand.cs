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

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Complete;

/// <summary>
/// ScholarshipProvider-side mark-as-complete for a ScholarshipProviderReviewRequest in UnderReview.
/// Captured payment is retained (no refund by default — spec PART 5). The
/// request moves UnderReview → Completed; the Closed state is reachable
/// admin-side and is intentionally not part of this command.
/// </summary>
[Auditable(AuditAction.Update, "ScholarshipProviderReviewRequest",
    TargetIdProperty = nameof(RequestId),
    SummaryTemplate = "ScholarshipProvider completed ScholarshipProviderReviewRequest {RequestId}")]
public sealed record CompleteScholarshipProviderReviewRequestCommand(Guid RequestId) : IRequest<bool>;

public sealed class CompleteScholarshipProviderReviewRequestCommandValidator
    : AbstractValidator<CompleteScholarshipProviderReviewRequestCommand>
{
    public CompleteScholarshipProviderReviewRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class CompleteScholarshipProviderReviewRequestCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<CompleteScholarshipProviderReviewRequestCommandHandler> logger)
    : IRequestHandler<CompleteScholarshipProviderReviewRequestCommand, bool>
{
    public async Task<bool> Handle(
        CompleteScholarshipProviderReviewRequestCommand command,
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

        if (entity.Status == ScholarshipProviderReviewRequestStatus.Completed ||
            entity.Status == ScholarshipProviderReviewRequestStatus.Closed)
        {
            return false;
        }

        if (entity.Status != ScholarshipProviderReviewRequestStatus.UnderReview)
            throw new ConflictException(
                $"Cannot complete a ScholarshipProviderReviewRequest in status {entity.Status} — only UnderReview requests can be completed.");

        entity.Status = ScholarshipProviderReviewRequestStatus.Completed;
        entity.CompletedAt = DateTimeOffset.UtcNow;

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
            NotificationType.ScholarshipProviderReviewRequestCompleted,
            paramsForStudent,
            deepLink: $"/student/review-requests/{entity.Id}",
            idempotencyKey: $"crr-completed-student:{entity.Id:N}",
            ct);

        await SafeNotificationDispatcher.TryDispatchAsync(
            notifications, logger,
            entity.ScholarshipProviderId,
            NotificationType.ScholarshipProviderReviewRequestCompleted,
            paramsForScholarshipProvider,
            deepLink: $"/company/review-requests/{entity.Id}",
            idempotencyKey: $"crr-completed-company:{entity.Id:N}",
            ct);

        logger.LogInformation(
            "ScholarshipProviderReviewRequest {RequestId} marked Completed by ScholarshipProvider.", entity.Id);

        return true;
    }
}
