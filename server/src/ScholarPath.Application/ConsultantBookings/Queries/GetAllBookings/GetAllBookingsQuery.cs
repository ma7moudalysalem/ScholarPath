using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetAllBookings;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists all consultant bookings platform-wide, newest first.
/// Restricted to Admin and SuperAdmin roles — a ScholarshipProvider has no
/// ownership relationship to any booking and must not see other parties' data.
/// </summary>
public sealed record GetAllBookingsQuery : IRequest<IReadOnlyList<BookingListItemDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetAllBookingsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetAllBookingsQuery, IReadOnlyList<BookingListItemDto>>
{
    public async Task<IReadOnlyList<BookingListItemDto>> Handle(
        GetAllBookingsQuery request, CancellationToken ct)
    {
        // SEC-03: only platform admins may list every booking. ScholarshipProvider
        // was previously allowed but has no ownership path to a booking, so it
        // leaked all students'/consultants' bookings platform-wide.
        var isPrivileged = currentUser.IsInRole("Admin")
            || currentUser.IsInRole("SuperAdmin");

        if (!isPrivileged)
            throw new ForbiddenAccessException("Only admins can view all bookings.");

        return await db.Bookings
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
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
                RequestedAt = b.RequestedAt,
                ConfirmedAt = b.ConfirmedAt,
                CreatedAt = b.CreatedAt,
            })
            .ToListAsync(ct);
    }
}
