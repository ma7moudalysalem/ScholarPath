using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetConsultantAvailability;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns a consultant's upcoming bookable slots — recurring + ad-hoc
/// availability rules expanded into concrete dated windows, with windows already
/// taken by a non-cancelled booking removed. Anonymous-accessible (students
/// browse this before booking). Throws <see cref="NotFoundException"/> when the
/// id is not an active consultant.
/// </summary>
public sealed record GetConsultantAvailabilityQuery(Guid ConsultantId)
    : IRequest<IReadOnlyList<BookableSlotDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetConsultantAvailabilityQueryHandler(IConsultantReadService consultants)
    : IRequestHandler<GetConsultantAvailabilityQuery, IReadOnlyList<BookableSlotDto>>
{
    public async Task<IReadOnlyList<BookableSlotDto>> Handle(
        GetConsultantAvailabilityQuery request, CancellationToken ct)
        => await consultants.GetConsultantOpenSlotsAsync(request.ConsultantId, ct)
           ?? throw new NotFoundException("Consultant", request.ConsultantId);
}
