using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetConsultantBookings;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Lists the authenticated consultant's own (incoming) bookings, newest first.
/// Read-only projection — backs the consultant-portal "Bookings" page.
/// </summary>
public sealed record GetConsultantBookingsQuery : IRequest<IReadOnlyList<BookingListItemDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetConsultantBookingsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetConsultantBookingsQuery, IReadOnlyList<BookingListItemDto>>
{
    public async Task<IReadOnlyList<BookingListItemDto>> Handle(
        GetConsultantBookingsQuery request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Consultant"))
        {
            throw new ForbiddenAccessException("Only consultants can view incoming bookings.");
        }

        var consultantId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await db.Bookings
            .AsNoTracking()
            .Where(b => b.ConsultantId == consultantId && !b.IsDeleted)
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
