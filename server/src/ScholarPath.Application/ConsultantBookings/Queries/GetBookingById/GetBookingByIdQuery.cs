using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetBookingById;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns a single booking's full detail. The caller must be the booking's
/// student, its consultant, or an admin — otherwise a
/// <see cref="ForbiddenAccessException"/> is thrown. A missing booking throws
/// <see cref="NotFoundException"/>.
/// </summary>
public sealed record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingDetailDto>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetBookingByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetBookingByIdQuery, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(GetBookingByIdQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var booking = await db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == request.BookingId && !b.IsDeleted)
            .Select(b => new BookingDetailDto
            {
                Id = b.Id,
                StudentId = b.StudentId,
                StudentName = b.Student!.FirstName + " " + b.Student.LastName,
                StudentEmail = b.Student.Email,
                StudentPhotoUrl = b.Student.ProfileImageUrl,
                ConsultantId = b.ConsultantId,
                ConsultantName = b.Consultant!.FirstName + " " + b.Consultant.LastName,
                ConsultantEmail = b.Consultant.Email,
                ConsultantPhotoUrl = b.Consultant.ProfileImageUrl,
                AvailabilityId = b.AvailabilityId,
                Status = b.Status,
                ScheduledStartAt = b.ScheduledStartAt,
                ScheduledEndAt = b.ScheduledEndAt,
                DurationMinutes = b.DurationMinutes,
                PriceUsd = b.PriceUsd,
                MeetingUrl = b.MeetingUrl,
                MeetingRoomId = b.MeetingRoomId,
                RequestedAt = b.RequestedAt,
                ConfirmedAt = b.ConfirmedAt,
                RejectedAt = b.RejectedAt,
                ExpiredAt = b.ExpiredAt,
                CancelledAt = b.CancelledAt,
                CompletedAt = b.CompletedAt,
                CancellationReason = b.CancellationReason,
                CancelledByUserId = b.CancelledByUserId,
                PaymentId = b.PaymentId,
                StripePaymentIntentId = b.StripePaymentIntentId,
                PaymentStatus = b.Payment != null ? b.Payment.Status : (PaymentStatus?)null,
                RefundedAmountCents = b.Payment != null ? b.Payment.RefundedAmountCents : (long?)null,
                RefundReason = b.Payment != null ? b.Payment.RefundReason : null,
                IsNoShowStudent = b.IsNoShowStudent,
                IsNoShowConsultant = b.IsNoShowConsultant,
                NoShowMarkedAt = b.NoShowMarkedAt,
                StudentJoinedAt = b.StudentJoinedAt,
                ConsultantJoinedAt = b.ConsultantJoinedAt,
                CreatedAt = b.CreatedAt,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(ConsultantBooking), request.BookingId);

        var isParticipant = booking.StudentId == userId || booking.ConsultantId == userId;
        var isAdmin = currentUser.IsInRole("Admin") || currentUser.IsInRole("SuperAdmin");

        if (!isParticipant && !isAdmin)
        {
            throw new ForbiddenAccessException("You are not allowed to view this booking.");
        }

        return booking;
    }
}
