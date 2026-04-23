using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;

public sealed class UpdateAvailabilityCommandHandler : IRequestHandler<UpdateAvailabilityCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateAvailabilityCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser; 
    }

    public async Task<Unit> Handle(UpdateAvailabilityCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        if (!_currentUser.IsInRole("Consultant"))
        {
            throw new UnauthorizedAccessException("Only consultants can update availability.");
        }

        var consultantId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var existingAvailabilities = _context.Availabilities
            .Where(a => a.ConsultantId == consultantId && !a.IsDeleted);

        if (request.ReplaceExisting)
        {
            await foreach (var availability in existingAvailabilities.AsAsyncEnumerable().WithCancellation(cancellationToken))
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
}
