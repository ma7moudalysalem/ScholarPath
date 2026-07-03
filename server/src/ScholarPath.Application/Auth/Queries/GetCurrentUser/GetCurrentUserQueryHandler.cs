using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAdministration userAdministration)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Include the Profile so we can surface the latest onboarding rejection
        // reason (AUTH-CODE-06) — it's the only screen that consistently needs
        // it, and the wizard renders the warning from this response.
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        var roles = await userAdministration.GetRolesAsync(user.Id, ct);

        return new CurrentUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.ProfileImageUrl,
            user.AccountStatus.ToString(),
            user.IsOnboardingComplete,
            user.EmailConfirmed,
            roles,
            user.ActiveRole,
            user.PreferredLanguage,
            user.Profile?.LastOnboardingRejectionReason,
            user.Profile?.LastOnboardingRejectedAt);
    }
}
