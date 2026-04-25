using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.SubmitConsultantRating;

[Auditable(
    AuditAction.ConsultantRatingSubmitted,
    "ConsultantBooking",
    TargetIdProperty = nameof(BookingId),
    SummaryTemplate = "Consultant rating submitted for booking: {TargetId}"
)]
public sealed record SubmitConsultantRatingCommand(
    Guid BookingId,
    int Rating,
    string? Comment
) : IRequest;
