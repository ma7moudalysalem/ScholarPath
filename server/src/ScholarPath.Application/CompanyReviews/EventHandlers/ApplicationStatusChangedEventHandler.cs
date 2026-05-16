using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.CompanyReviews.Commands.CaptureCompanyReviewPayment;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.CompanyReviews.EventHandlers;

public sealed class ApplicationStatusChangedEventHandler(
    ISender sender,
    ILogger<ApplicationStatusChangedEventHandler> logger)
    : INotificationHandler<ApplicationStatusChangedEvent>
{
    public async Task Handle(ApplicationStatusChangedEvent notification, CancellationToken ct)
    {
        if (notification.NewStatus is ApplicationStatus.Accepted or ApplicationStatus.Rejected)
        {
            logger.LogInformation("Application {ApplicationId} reached final status {Status}. Triggering review payment capture.", 
                notification.ApplicationId, notification.NewStatus);

            var captureCommand = new CaptureCompanyReviewPaymentCommand(notification.ApplicationId);
            await sender.Send(captureCommand, ct);
        }
    }
}
