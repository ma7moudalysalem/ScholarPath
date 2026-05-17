using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetMyBookings;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists the authenticated student's consultant bookings, newest first.
/// Read-only projection (<c>AsNoTracking</c>) — mirrors the Scholarships query
/// pattern. Backs the student "My bookings" page.
/// </summary>
public sealed record GetMyBookingsQuery : IRequest<IReadOnlyList<BookingListItemDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetMyBookingsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyBookingsQuery, IReadOnlyList<BookingListItemDto>>
{
    public async Task<IReadOnlyList<BookingListItemDto>> Handle(
        GetMyBookingsQuery request, CancellationToken ct)
    {
        var studentId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await db.Bookings
            .AsNoTracking()
            .Where(b => b.StudentId == studentId && !b.IsDeleted)
            .OrderByDescending(b => b.ScheduledStartAt)
            .ThenByDescending(b => b.Id)
            .Select(b => new BookingListItemDto
            {
                Id = b.Id,
                StudentId = b.StudentId,
                StudentName = b.Student!.FirstName + " " + b.Student.LastName,
                StudentEmail = b.Student.Email,
                ConsultantId = b.ConsultantId,
                ConsultantName = b.Consultant!.FirstName + " " + b.Consultant.LastName,
                ConsultantPhotoUrl = b.Consultant.ProfileImageUrl,
                Status = b.Status,
                ScheduledStartAt = b.ScheduledStartAt,
                ScheduledEndAt = b.ScheduledEndAt,
                DurationMinutes = b.DurationMinutes,
                PriceUsd = b.PriceUsd,
                MeetingUrl = b.MeetingUrl,
                RequestedAt = b.RequestedAt,
                ConfirmedAt = b.ConfirmedAt,
                CreatedAt = b.CreatedAt,
            })
            .ToListAsync(ct);
    }
}
