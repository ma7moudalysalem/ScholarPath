using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;

public sealed class SubmitCompanyRatingCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<SubmitCompanyRatingCommandHandler> logger)
    : IRequestHandler<SubmitCompanyRatingCommand, Guid>
{
    public async Task<Guid> Handle(SubmitCompanyRatingCommand request, CancellationToken ct)
    {
        var application = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(ApplicationTracker), request.ApplicationId);

        if (application.StudentId != currentUser.UserId)
        {
            throw new ForbiddenAccessException();
        }

        if (application.Status is not (ApplicationStatus.Accepted or ApplicationStatus.Rejected))
        {
            throw new ConflictException("Application must be in a final state to submit a review.");
        }

        var existingReview = await db.CompanyReviews
            .AnyAsync(r => r.ApplicationTrackerId == request.ApplicationId, ct);

        if (existingReview)
        {
            throw new ConflictException("A review has already been submitted for this application.");
        }

        var review = new CompanyReview
        {
            ApplicationTrackerId = request.ApplicationId,
            StudentId = (currentUser.UserId ?? throw new ForbiddenAccessException()),
            CompanyId = request.CompanyId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.CompanyReviews.Add(review);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await notifications.DispatchAsync(
            request.CompanyId,
            NotificationType.CompanyRatingReceived,
            new NotificationContent("New Rating", "تقييم جديد", $"You received a {request.Rating}-star rating.", $"لقد حصلت على تقييم {request.Rating} نجوم.", null),            null,
            null,
            ct);

        logger.LogInformation("Student {StudentId} submitted a {Rating}-star rating for company {CompanyId}",
            currentUser.UserId, request.Rating, request.CompanyId);

        return review.Id;
    }
}
