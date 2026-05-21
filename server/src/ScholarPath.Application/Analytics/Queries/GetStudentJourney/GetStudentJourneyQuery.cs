using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Analytics.Queries.GetStudentJourney;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record StudentJourneyDto(
    int TotalApplications,
    int SubmittedApplications,
    int AcceptedApplications,
    int TotalBookings,
    int CompletedBookings,
    DateTime? LastApplicationAt,
    DateTime? LastBookingAt,
    bool OnboardingComplete);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns the calling student's journey aggregates from <c>dbo.vw_student_journey</c>.
/// Returns an all-zeros DTO when the student has no activity yet.
/// </summary>
public record GetStudentJourneyQuery : IRequest<StudentJourneyDto>;

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetStudentJourneyQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetStudentJourneyQuery, StudentJourneyDto>
{
    private sealed record StudentJourneyRow(
        int TotalApplications,
        int SubmittedApplications,
        int AcceptedApplications,
        int TotalBookings,
        int CompletedBookings,
        DateTime? LastApplicationAt,
        DateTime? LastBookingAt,
        bool OnboardingComplete);

    public async Task<StudentJourneyDto> Handle(
        GetStudentJourneyQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var rows = await db.Database
            .SqlQuery<StudentJourneyRow>(
                $"""
                SELECT
                    TotalApplications,
                    SubmittedApplications,
                    AcceptedApplications,
                    TotalBookings,
                    CompletedBookings,
                    LastApplicationAt,
                    LastBookingAt,
                    OnboardingComplete
                FROM dbo.vw_student_journey
                WHERE StudentId = {userId}
                """)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return new StudentJourneyDto(0, 0, 0, 0, 0, null, null, false);

        var r = rows[0];
        return new StudentJourneyDto(
            r.TotalApplications,
            r.SubmittedApplications,
            r.AcceptedApplications,
            r.TotalBookings,
            r.CompletedBookings,
            r.LastApplicationAt,
            r.LastBookingAt,
            r.OnboardingComplete);
    }
}
