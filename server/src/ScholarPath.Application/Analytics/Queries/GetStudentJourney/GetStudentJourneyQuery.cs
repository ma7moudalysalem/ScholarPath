using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Analytics.Queries.GetStudentJourney;

public record StudentJourneyDto(
    int TotalApplications,
    int SubmittedApplications,
    int AcceptedApplications,
    int TotalBookings,
    int CompletedBookings,
    DateTime? LastApplicationAt,
    DateTime? LastBookingAt,
    bool OnboardingComplete);

public record GetStudentJourneyQuery : IRequest<StudentJourneyDto>;

public sealed class GetStudentJourneyQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetStudentJourneyQuery, StudentJourneyDto>
{
    public async Task<StudentJourneyDto> Handle(
        GetStudentJourneyQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var apps = await db.Applications
            .AsNoTracking()
            .Where(a => a.StudentId == userId && !a.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Submitted = g.Count(a => a.SubmittedAt != null),
                Accepted = g.Count(a => a.Status == ApplicationStatus.Accepted),
                LastAt = (DateTimeOffset?)g.Max(a => a.CreatedAt),
            })
            .FirstOrDefaultAsync(ct);

        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(b => b.StudentId == userId && !b.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Completed = g.Count(b => b.Status == BookingStatus.Completed),
                LastAt = (DateTimeOffset?)g.Max(b => b.CreatedAt),
            })
            .FirstOrDefaultAsync(ct);

        var onboardingComplete = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.IsOnboardingComplete)
            .FirstOrDefaultAsync(ct);

        return new StudentJourneyDto(
            TotalApplications:     apps?.Total ?? 0,
            SubmittedApplications: apps?.Submitted ?? 0,
            AcceptedApplications:  apps?.Accepted ?? 0,
            TotalBookings:         bookings?.Total ?? 0,
            CompletedBookings:     bookings?.Completed ?? 0,
            LastApplicationAt:     apps?.LastAt?.UtcDateTime,
            LastBookingAt:         bookings?.LastAt?.UtcDateTime,
            OnboardingComplete:    onboardingComplete);
    }
}
