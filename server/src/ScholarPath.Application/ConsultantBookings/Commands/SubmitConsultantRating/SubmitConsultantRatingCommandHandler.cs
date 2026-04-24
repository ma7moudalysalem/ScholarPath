using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.SubmitConsultantRating;

public sealed class SubmitConsultantRatingCommandHandler : IRequestHandler<SubmitConsultantRatingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitConsultantRatingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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
            throw new InvalidOperationException("Booking was not found.");
        }

        if (booking.StudentId != currentUserId)
        {
            throw new UnauthorizedAccessException("Only the student of this booking can submit a rating.");
        }

        if (booking.Status != BookingStatus.Completed)
        {
            throw new InvalidOperationException("Consultant rating can only be submitted for completed bookings.");
        }

        var alreadyRated = await _context.ConsultantReviews
            .AnyAsync(r => r.BookingId == request.BookingId && !r.IsDeleted, cancellationToken);

        if (alreadyRated)
        {
            throw new InvalidOperationException("A consultant rating has already been submitted for this booking.");
        }

        var consultant = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == booking.ConsultantId, cancellationToken);

        if (consultant is null)
        {
            throw new InvalidOperationException("Consultant user was not found.");
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

        var last20Ratings = await _context.ConsultantReviews
            .Where(r =>
                r.ConsultantId == booking.ConsultantId &&
                !r.IsDeleted &&
                !r.IsHiddenByAdmin)
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        if (last20Ratings.Count == 20)
        {
            var average = last20Ratings.Average();

            if (average < 3.0)
            {
                consultant.AccountStatus = AccountStatus.Suspended;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
