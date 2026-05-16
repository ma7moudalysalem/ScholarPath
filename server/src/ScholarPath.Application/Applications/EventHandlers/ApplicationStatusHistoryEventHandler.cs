using System.Text.Json;
using MediatR;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.Applications.EventHandlers;

/// <summary>
/// Records every ApplicationTracker status transition as a "StatusHistory" child row
/// so the student (and company) get a visible application timeline. Runs for any
/// handler that raises <see cref="ApplicationStatusChangedEvent"/> — submit, review,
/// withdraw — so the history can never silently diverge from the actual status.
/// </summary>
public sealed class ApplicationStatusHistoryEventHandler(IApplicationDbContext db)
    : INotificationHandler<ApplicationStatusChangedEvent>
{
    public async Task Handle(ApplicationStatusChangedEvent notification, CancellationToken ct)
    {
        db.ApplicationChildren.Add(new ApplicationTrackerChild
        {
            Id = Guid.NewGuid(),
            ApplicationTrackerId = notification.ApplicationId,
            ChildType = "StatusHistory",
            Title = notification.NewStatus.ToString(),
            Content = $"{notification.OldStatus} → {notification.NewStatus}",
            MetadataJson = JsonSerializer.Serialize(new
            {
                from = notification.OldStatus.ToString(),
                to = notification.NewStatus.ToString(),
            }),
            OccurredAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }
}
