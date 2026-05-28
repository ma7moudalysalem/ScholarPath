using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;

public sealed class ConfigureReviewFeeCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<ConfigureReviewFeeCommandHandler> logger)
    : IRequestHandler<ConfigureReviewFeeCommand, bool>
{
    public async Task<bool> Handle(ConfigureReviewFeeCommand request, CancellationToken ct)
    {
        var scholarship = await db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Scholarship), request.ScholarshipId);

        // Verify the current user is the owner or an admin
        if (scholarship.OwnerCompanyId != currentUser.UserId && !currentUser.IsInRole("Admin"))
        {
            throw new ForbiddenAccessException();
        }

        // Master switch: when payments are off platform-wide, the requested
        // value is ignored and the fee is forced to 0 — the Apply Now flow
        // then runs free-mode for this listing.
        var paymentsEnabled = await PlatformSettingsReader.GetBooleanAsync(
            db, PlatformSettingsKeys.PaymentsEnabled, defaultValue: true, ct);
        if (!paymentsEnabled)
        {
            scholarship.ReviewFeeUsd = 0m;
        }
        else
        {
            // Settings gate: free in-app scholarships can be disabled platform-wide.
            if (request.ReviewFeeUsd == 0m)
            {
                var freeAllowed = await PlatformSettingsReader.GetBooleanAsync(
                    db, PlatformSettingsKeys.AllowFreeScholarships, defaultValue: true, ct);
                if (!freeAllowed)
                    throw new ConflictException(
                        "Free in-app scholarships are not enabled on this platform. Please set a Review Service Fee greater than 0.");
            }
            scholarship.ReviewFeeUsd = request.ReviewFeeUsd;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Review fee for scholarship {ScholarshipId} set to {Fee}", request.ScholarshipId, request.ReviewFeeUsd);

        return true;
    }
}
