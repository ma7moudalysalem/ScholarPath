using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Infrastructure.Jobs;

public sealed class CompletionJob : ICompletionJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CompletionJob> _logger;
    private readonly IPublisher _publisher;

    public CompletionJob(
        IApplicationDbContext context,
        ILogger<CompletionJob> logger,
        IPublisher publisher)
    {
        _context = context;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var completionThreshold = DateTimeOffset.UtcNow.AddHours(-6);

        var bookings = await _context.Bookings
            .Where(b =>
                b.Status == BookingStatus.Confirmed &&
                b.ScheduledEndAt <= completionThreshold &&
                !b.IsNoShowStudent &&
                !b.IsNoShowConsultant &&
                // Never auto-complete a booking where exactly ONE party joined — that's
                // a no-show and belongs to MeetingNoShowSweepJob (which files a report).
                // Completing it here would silently erase the present party's no-show
                // remedy if the sweep lagged past this 6h threshold. Both-joined or
                // neither-joined bookings still complete normally.
                !((b.StudentJoinedAt != null && b.ConsultantJoinedAt == null) ||
                  (b.StudentJoinedAt == null && b.ConsultantJoinedAt != null)))
            .ToListAsync(cancellationToken);

        if (bookings.Count == 0)
        {
            _logger.LogInformation("CompletionJob found no bookings eligible for auto-completion.");
            return;
        }

        var nowUtc = DateTimeOffset.UtcNow;

        foreach (var booking in bookings)
        {
            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = nowUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var booking in bookings)
        {
            await _publisher.Publish(
                new BookingCompletedEvent(
                    booking.Id,
                    booking.StudentId,
                    booking.ConsultantId),
                cancellationToken);
        }

        _logger.LogInformation("CompletionJob auto-completed {Count} bookings.", bookings.Count);
    }
}
