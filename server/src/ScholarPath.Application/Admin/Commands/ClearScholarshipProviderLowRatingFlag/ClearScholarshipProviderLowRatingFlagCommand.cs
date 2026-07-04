using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Commands.ClearScholarshipProviderLowRatingFlag;

/// <summary>
/// Admin's "reviewed, no further action" exit from the low-rated companies
/// queue. Clears <c>UserProfile.ScholarshipProviderLowRatingFlaggedAt</c> back to null so
/// the row disappears from <c>GetLowRatedCompaniesQuery</c>. The ScholarshipProvider's
/// rating snapshot (average + count) is left untouched — only the flag is
/// cleared. Distinct from suspension, which uses the existing
/// <c>SetUserStatusCommand</c>.
///
/// Idempotent: returns false if the company isn't currently flagged.
/// </summary>
[Auditable(AuditAction.Update, "UserProfile",
    TargetIdProperty = nameof(ScholarshipProviderId),
    SummaryTemplate = "Admin cleared low-rating flag on company {ScholarshipProviderId}")]
public sealed record ClearScholarshipProviderLowRatingFlagCommand(Guid ScholarshipProviderId)
    : IRequest<bool>;

public sealed class ClearScholarshipProviderLowRatingFlagCommandValidator
    : AbstractValidator<ClearScholarshipProviderLowRatingFlagCommand>
{
    public ClearScholarshipProviderLowRatingFlagCommandValidator()
    {
        RuleFor(x => x.ScholarshipProviderId).NotEmpty();
    }
}

public sealed class ClearScholarshipProviderLowRatingFlagCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<ClearScholarshipProviderLowRatingFlagCommandHandler> logger)
    : IRequestHandler<ClearScholarshipProviderLowRatingFlagCommand, bool>
{
    public async Task<bool> Handle(
        ClearScholarshipProviderLowRatingFlagCommand command, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
        {
            throw new ForbiddenAccessException(
                "Only an administrator can clear the low-rating flag.");
        }

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == command.ScholarshipProviderId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.UserProfile), command.ScholarshipProviderId);

        if (profile.ScholarshipProviderLowRatingFlaggedAt is null)
        {
            // Already cleared — idempotent.
            return false;
        }

        profile.ScholarshipProviderLowRatingFlaggedAt = null;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Admin {AdminId} cleared low-rating flag on company {ScholarshipProviderId}.",
            currentUser.UserId, command.ScholarshipProviderId);

        return true;
    }
}
