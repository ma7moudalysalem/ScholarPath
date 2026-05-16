using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        scholarship.ReviewFeeUsd = request.ReviewFeeUsd;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Review fee for scholarship {ScholarshipId} set to {Fee}", request.ScholarshipId, request.ReviewFeeUsd);

        return true;
    }
}
