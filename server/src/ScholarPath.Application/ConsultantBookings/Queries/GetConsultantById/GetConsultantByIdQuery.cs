using MediatR;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetConsultantById;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns one consultant's full public profile detail. Anonymous-accessible.
/// Throws <see cref="NotFoundException"/> when the id is not an active user in
/// the <c>Consultant</c> role.
/// </summary>
public sealed record GetConsultantByIdQuery(Guid ConsultantId) : IRequest<ConsultantDetailDto>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetConsultantByIdQueryHandler(IConsultantReadService consultants)
    : IRequestHandler<GetConsultantByIdQuery, ConsultantDetailDto>
{
    public async Task<ConsultantDetailDto> Handle(
        GetConsultantByIdQuery request, CancellationToken ct)
        => await consultants.GetConsultantDetailAsync(request.ConsultantId, ct)
           ?? throw new NotFoundException("Consultant", request.ConsultantId);
}
