using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Commands.ClearCompanyLowRatingFlag;

/// <summary>
/// Admin's "reviewed, no further action" exit from the low-rated companies
/// queue. Clears <c>UserProfile.CompanyLowRatingFlaggedAt</c> back to null so
/// the row disappears from <c>GetLowRatedCompaniesQuery</c>. The Company's
/// rating snapshot (average + count) is left untouched — only the flag is
/// cleared. Distinct from suspension, which uses the existing
/// <c>SetUserStatusCommand</c>.
///
/// Idempotent: returns false if the company isn't currently flagged.
/// </summary>
[Auditable(AuditAction.Update, "UserProfile",
    TargetIdProperty = nameof(CompanyId),
    SummaryTemplate = "Admin cleared low-rating flag on company {CompanyId}")]
public sealed record ClearCompanyLowRatingFlagCommand(Guid CompanyId)
    : IRequest<bool>;

public sealed class ClearCompanyLowRatingFlagCommandValidator
    : AbstractValidator<ClearCompanyLowRatingFlagCommand>
{
    public ClearCompanyLowRatingFlagCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public sealed class ClearCompanyLowRatingFlagCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<ClearCompanyLowRatingFlagCommandHandler> logger)
    : IRequestHandler<ClearCompanyLowRatingFlagCommand, bool>
{
    public async Task<bool> Handle(
        ClearCompanyLowRatingFlagCommand command, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("SuperAdmin"))
        {
            throw new ForbiddenAccessException(
                "Only an administrator can clear the low-rating flag.");
        }

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == command.CompanyId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.UserProfile), command.CompanyId);

        if (profile.CompanyLowRatingFlaggedAt is null)
        {
            // Already cleared — idempotent.
            return false;
        }

        profile.CompanyLowRatingFlaggedAt = null;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Admin {AdminId} cleared low-rating flag on company {CompanyId}.",
            currentUser.UserId, command.CompanyId);

        return true;
    }
}
