using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Commands.ApproveScholarship;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>Admin approves an under-review scholarship — moves it to <c>Open</c>.</summary>
[Auditable(AuditAction.Approved, "Scholarship",
    TargetIdProperty = nameof(ScholarshipId),
    SummaryTemplate = "Approved scholarship {ScholarshipId}")]
public sealed record ApproveScholarshipCommand(Guid ScholarshipId) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ApproveScholarshipCommandValidator : AbstractValidator<ApproveScholarshipCommand>
{
    public ApproveScholarshipCommandValidator() => RuleFor(x => x.ScholarshipId).NotEmpty();
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ApproveScholarshipCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ILogger<ApproveScholarshipCommandHandler> logger)
    : IRequestHandler<ApproveScholarshipCommand, bool>
{
    public async Task<bool> Handle(ApproveScholarshipCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only an administrator can approve scholarships.");

        var adminId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var scholarship = await db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == request.ScholarshipId && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);

        if (scholarship.Status != ScholarshipStatus.UnderReview)
            throw new ConflictException("Only a scholarship under review can be approved.");

        scholarship.Status = ScholarshipStatus.Open;
        scholarship.OpenedAt ??= DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Scholarship {ScholarshipId} approved by {AdminId}.", scholarship.Id, adminId);
        return true;
    }
}
