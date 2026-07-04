using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.SubmitConsultantRating;

public sealed class SubmitConsultantRatingCommandHandler : IRequestHandler<SubmitConsultantRatingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SubmitConsultantRatingCommandHandler> _logger;
    private readonly BookingOptions _bookingOptions;

    public SubmitConsultantRatingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IOptions<BookingOptions> bookingOptions,
        ILogger<SubmitConsultantRatingCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _bookingOptions = bookingOptions.Value;
        _logger = logger;
    }

    public async Task Handle(SubmitConsultantRatingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var currentUserId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingDomainException("Booking was not found.");
        }

        if (booking.StudentId != currentUserId)
        {
            throw new UnauthorizedAccessException("Only the student of this booking can submit a rating.");
        }

        if (booking.Status != BookingStatus.Completed)
        {
            throw new BookingDomainException("Consultant rating can only be submitted for completed bookings.");
        }

        var alreadyRated = await _context.ConsultantReviews
            .AnyAsync(r => r.BookingId == request.BookingId && !r.IsDeleted, cancellationToken);

        if (alreadyRated)
        {
            throw new BookingDomainException("A consultant rating has already been submitted for this booking.");
        }

        var consultant = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == booking.ConsultantId, cancellationToken);

        if (consultant is null)
        {
            throw new BookingDomainException("Consultant user was not found.");
        }

        var review = new ConsultantReview
        {
            BookingId = booking.Id,
            StudentId = booking.StudentId,
            ConsultantId = booking.ConsultantId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            IsHiddenByAdmin = false,
            AdminNote = null,
            IsDeleted = false,
            DeletedAt = null,
            DeletedByUserId = null
        };

        _context.ConsultantReviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        // FR-094: when the consultant's recent ratings fall below the configured
        // threshold, auto-suspend their *booking intake* (not the whole account)
        // pending admin review — the consultant keeps full access otherwise.
        var windowSize = _bookingOptions.LowRatingWindowSize;
        var recentRatings = await _context.ConsultantReviews
            .Where(r =>
                r.ConsultantId == booking.ConsultantId &&
                !r.IsDeleted &&
                !r.IsHiddenByAdmin)
            .OrderByDescending(r => r.CreatedAt)
            .Take(windowSize)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        // FR-CBR-37: evaluate the average over *up to* the last 20 visible ratings —
        // a consultant does not need a full window of 20 before they can be flagged,
        // only a minimum sample big enough to be meaningful.
        if (recentRatings.Count >= _bookingOptions.LowRatingMinimumSampleSize)
        {
            var average = recentRatings.Average();

            if (average < _bookingOptions.LowRatingThreshold
                && consultant.Profile is { BookingIntakeSuspendedAt: null } profile)
            {
                profile.BookingIntakeSuspendedAt = DateTimeOffset.UtcNow;

                _logger.LogWarning(
                    "Auto-suspended booking intake for consultant {ConsultantId} " +
                    "(avg rating {Avg} over last {Window} sessions, threshold {Threshold})",
                    consultant.Id, average, windowSize, _bookingOptions.LowRatingThreshold);

                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
