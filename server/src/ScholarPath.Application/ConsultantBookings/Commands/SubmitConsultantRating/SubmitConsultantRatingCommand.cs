using MediatR;

namespace ScholarPath.Application.ConsultantBookings.Commands.SubmitConsultantRating;

public sealed record SubmitConsultantRatingCommand(
    Guid BookingId,
    int Rating,
    string? Comment
) : IRequest;
