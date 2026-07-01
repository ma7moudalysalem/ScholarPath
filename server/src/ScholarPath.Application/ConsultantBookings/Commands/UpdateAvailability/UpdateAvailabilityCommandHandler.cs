using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;

public sealed class UpdateAvailabilityCommandHandler : IRequestHandler<UpdateAvailabilityCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IConsultantEligibilityService _consultantEligibility;

    public UpdateAvailabilityCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IConsultantEligibilityService consultantEligibility)
    {
        _context = context;
        _currentUser = currentUser;
        _consultantEligibility = consultantEligibility;
    }

    public async Task<Unit> Handle(UpdateAvailabilityCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var consultantId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

        // Availability is a consultant-only capability, and holding the
        // Consultant role is not enough — the account must be a verified/approved
        // consultant. Without this, a stale Consultant role would let a student
        // publish availability and surface themselves in the marketplace.
        if (!await _consultantEligibility.CanActAsConsultantAsync(consultantId, cancellationToken))
        {
            throw new ForbiddenAccessException(
                "Only verified consultants can manage availability. Your consultant access is not active.");
        }

        var existingAvailabilities = await _context.Availabilities
            .Where(a => a.ConsultantId == consultantId && !a.IsDeleted && a.IsActive)
            .ToListAsync(cancellationToken);

        if (!request.ReplaceExisting)
        {
            ValidateRecurringSlotsAgainstExisting(request.Slots, existingAvailabilities);
            ValidateAdHocSlotsAgainstExisting(request.Slots, existingAvailabilities);
        }

        if (request.ReplaceExisting)
        {
            foreach (var availability in existingAvailabilities)
            {
                availability.IsDeleted = true;
                availability.DeletedAt = DateTimeOffset.UtcNow;
                availability.DeletedByUserId = consultantId;
                availability.IsActive = false;
            }
        }

        foreach (var slot in request.Slots)
        {
            var availability = new ConsultantAvailability
            {
                ConsultantId = consultantId,
                IsRecurring = slot.IsRecurring,
                DayOfWeek = slot.IsRecurring ? slot.DayOfWeek : null,
                StartTime = slot.IsRecurring ? slot.StartTime : null,
                EndTime = slot.IsRecurring ? slot.EndTime : null,
                SpecificStartAt = !slot.IsRecurring ? slot.SpecificStartAt?.ToUniversalTime() : null,
                SpecificEndAt = !slot.IsRecurring ? slot.SpecificEndAt?.ToUniversalTime() : null,
                Timezone = slot.Timezone,
                IsActive = slot.IsActive,
                IsDeleted = false,
                DeletedAt = null,
                DeletedByUserId = null
            };

            _context.Availabilities.Add(availability);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static void ValidateRecurringSlotsAgainstExisting(
        List<AvailabilityInputModel> newSlots,
        List<ConsultantAvailability> existingAvailabilities)
    {
        var newRecurringSlots = newSlots
            .Where(x => x.IsRecurring && x.DayOfWeek.HasValue && x.StartTime.HasValue && x.EndTime.HasValue)
            .ToList();

        var existingRecurringSlots = existingAvailabilities
            .Where(x => x.IsRecurring && x.DayOfWeek.HasValue && x.StartTime.HasValue && x.EndTime.HasValue)
            .ToList();

        foreach (var newSlot in newRecurringSlots)
        {
            foreach (var existingSlot in existingRecurringSlots)
            {
                if (newSlot.DayOfWeek != existingSlot.DayOfWeek)
                {
                    continue;
                }

                var newStart = newSlot.StartTime!.Value;
                var newEnd = newSlot.EndTime!.Value;
                var existingStart = existingSlot.StartTime!.Value;
                var existingEnd = existingSlot.EndTime!.Value;

                var overlaps = newStart < existingEnd && newEnd > existingStart;

                if (overlaps)
                {
                    throw new BookingDomainException(
                        "One or more recurring availability slots overlap with existing saved availability.");
                }
            }
        }
    }

    private static void ValidateAdHocSlotsAgainstExisting(
        List<AvailabilityInputModel> newSlots,
        List<ConsultantAvailability> existingAvailabilities)
    {
        var newAdHocSlots = newSlots
            .Where(x => !x.IsRecurring && x.SpecificStartAt.HasValue && x.SpecificEndAt.HasValue)
            .ToList();

        var existingAdHocSlots = existingAvailabilities
            .Where(x => !x.IsRecurring && x.SpecificStartAt.HasValue && x.SpecificEndAt.HasValue)
            .ToList();

        foreach (var newSlot in newAdHocSlots)
        {
            foreach (var existingSlot in existingAdHocSlots)
            {
                var newStart = newSlot.SpecificStartAt!.Value.ToUniversalTime();
                var newEnd = newSlot.SpecificEndAt!.Value.ToUniversalTime();
                var existingStart = existingSlot.SpecificStartAt!.Value.ToUniversalTime();
                var existingEnd = existingSlot.SpecificEndAt!.Value.ToUniversalTime();

                var overlaps = newStart < existingEnd && newEnd > existingStart;

                if (overlaps)
                {
                    throw new BookingDomainException(
                        "One or more ad-hoc availability slots overlap with existing saved availability.");
                }
            }
        }
    }
}
