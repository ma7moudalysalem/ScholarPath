using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.ReinstateBookingIntake;

/// <summary>
/// Admin command (FR-094): clears a consultant's auto-suspended booking intake
/// after review, so the consultant can receive new bookings again. The
/// account itself was never suspended — only new-booking intake was paused.
/// </summary>
[Auditable(AuditAction.Update, "User",
    TargetIdProperty = nameof(ConsultantId),
    SummaryTemplate = "Reinstated booking intake for consultant {ConsultantId}")]
public sealed record ReinstateBookingIntakeCommand(Guid ConsultantId) : IRequest;

public sealed class ReinstateBookingIntakeCommandValidator
    : AbstractValidator<ReinstateBookingIntakeCommand>
{
    public ReinstateBookingIntakeCommandValidator() =>
        RuleFor(x => x.ConsultantId).NotEmpty();
}

public sealed class ReinstateBookingIntakeCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ReinstateBookingIntakeCommand>
{
    public async Task Handle(ReinstateBookingIntakeCommand request, CancellationToken ct)
    {
        var consultant = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.ConsultantId, ct)
            ?? throw new NotFoundException("User", request.ConsultantId);

        // Clear BOTH the intake suspension AND the sticky low-rating admin flag —
        // an admin reinstating a consultant has reviewed them, so the flag (which
        // otherwise is never cleared anywhere, keeping the consultant permanently in
        // the low-rated queue and suppressing all future re-alerts) is resolved here too.
        if (consultant.Profile is { } profile
            && (profile.BookingIntakeSuspendedAt is not null || profile.ConsultantLowRatingFlaggedAt is not null))
        {
            profile.BookingIntakeSuspendedAt = null;
            profile.ConsultantLowRatingFlaggedAt = null;
            await db.SaveChangesAsync(ct);
        }
    }
}
