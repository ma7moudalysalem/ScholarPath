using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.ScholarshipProviderReviews.Commands.CaptureScholarshipProviderReviewPayment;
using ScholarPath.Application.ScholarshipProviderReviews.Commands.RejectScholarshipProviderReviewPayment;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.ScholarshipProviderReviews.EventHandlers;

/// <summary>
/// Drives the ScholarshipProviderReview payment outcome from the application's final status:
/// <list type="bullet">
///   <item>Accepted — capture the held PaymentIntent (company earns the fee).</item>
///   <item>Rejected — cancel the held PaymentIntent (no charge ever lands).</item>
/// </list>
/// Other intermediate states (UnderReview, Shortlisted, …) are ignored — the
/// hold remains in place while the company reviews.
/// </summary>
public sealed class ApplicationStatusChangedEventHandler(
    ISender sender,
    ILogger<ApplicationStatusChangedEventHandler> logger)
    : INotificationHandler<ApplicationStatusChangedEvent>
{
    public async Task Handle(ApplicationStatusChangedEvent notification, CancellationToken ct)
    {
        switch (notification.NewStatus)
        {
            case ApplicationStatus.Accepted:
                logger.LogInformation(
                    "Application {ApplicationId} accepted — capturing ScholarshipProviderReview payment.",
                    notification.ApplicationId);
                await sender.Send(
                    new CaptureScholarshipProviderReviewPaymentCommand(notification.ApplicationId), ct);
                break;

            case ApplicationStatus.Rejected:
                logger.LogInformation(
                    "Application {ApplicationId} rejected — cancelling held ScholarshipProviderReview payment.",
                    notification.ApplicationId);
                await sender.Send(
                    new RejectScholarshipProviderReviewPaymentCommand(notification.ApplicationId), ct);
                break;
        }
    }
}
