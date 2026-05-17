using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetMyAvailability;

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns the authenticated consultant's own active availability rules
/// (recurring + ad-hoc). Backs the consultant-portal "Availability" page so it
/// can render the rules the consultant has saved via
/// <c>UpdateAvailabilityCommand</c>.
/// </summary>
public sealed record GetMyAvailabilityQuery : IRequest<IReadOnlyList<AvailabilityRuleDto>>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetMyAvailabilityQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyAvailabilityQuery, IReadOnlyList<AvailabilityRuleDto>>
{
    public async Task<IReadOnlyList<AvailabilityRuleDto>> Handle(
        GetMyAvailabilityQuery request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Consultant"))
        {
            throw new ForbiddenAccessException("Only consultants have availability rules.");
        }

        var consultantId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        return await db.Availabilities
            .AsNoTracking()
            .Where(a => a.ConsultantId == consultantId && !a.IsDeleted && a.IsActive)
            .OrderBy(a => a.IsRecurring ? 0 : 1)
            .ThenBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ThenBy(a => a.SpecificStartAt)
            .Select(a => new AvailabilityRuleDto
            {
                Id = a.Id,
                ConsultantId = a.ConsultantId,
                IsRecurring = a.IsRecurring,
                DayOfWeek = a.DayOfWeek,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                SpecificStartAt = a.SpecificStartAt,
                SpecificEndAt = a.SpecificEndAt,
                Timezone = a.Timezone,
                IsActive = a.IsActive,
            })
            .ToListAsync(ct);
    }
}
