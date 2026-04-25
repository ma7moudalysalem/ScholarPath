using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Commands.SetRedactionSampleVerdict;

public sealed class SetRedactionSampleVerdictCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock)
    : IRequestHandler<SetRedactionSampleVerdictCommand, Unit>
{
    public async Task<Unit> Handle(SetRedactionSampleVerdictCommand request, CancellationToken ct)
    {
        var reviewerId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var sample = await db.AiRedactionAuditSamples
            .FirstOrDefaultAsync(s => s.Id == request.SampleId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(AiRedactionAuditSample), request.SampleId);

        sample.Verdict = request.Verdict;
        sample.ReviewerUserId = reviewerId;
        sample.ReviewedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
