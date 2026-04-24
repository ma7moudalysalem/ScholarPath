using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Jobs;

public sealed class CompletionJob : ICompletionJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CompletionJob> _logger;

    public CompletionJob(
        IApplicationDbContext context,
        ILogger<CompletionJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var completionThreshold = DateTimeOffset.UtcNow.AddHours(-6);

        var bookings = await _context.Bookings
            .Where(b =>
                b.Status == BookingStatus.Confirmed &&
                b.ScheduledEndAt <= completionThreshold &&
                !b.IsNoShowStudent &&
                !b.IsNoShowConsultant)
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

        _logger.LogInformation("CompletionJob auto-completed {Count} bookings.", bookings.Count);
    }
}
