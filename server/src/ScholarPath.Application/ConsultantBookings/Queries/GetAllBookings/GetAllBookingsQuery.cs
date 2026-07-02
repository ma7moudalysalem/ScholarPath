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
/// Restricted to Admin, SuperAdmin, and ScholarshipProvider roles.
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
        var isPrivileged = currentUser.IsInRole("Admin")
            || currentUser.IsInRole("SuperAdmin")
            || currentUser.IsInRole("ScholarshipProvider");

        if (!isPrivileged)
            throw new ForbiddenAccessException("Only admins and company users can view all bookings.");

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
