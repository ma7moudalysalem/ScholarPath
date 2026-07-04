using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Scholarships.Commands;

public record UpdateScholarshipCommand : IRequest<bool>
{
    public Guid Id { get; init; }
    public string TitleEn { get; init; } = default!;
    public string TitleAr { get; init; } = default!;
    public string DescriptionEn { get; init; } = default!;
    public string DescriptionAr { get; init; } = default!;
    public DateTimeOffset Deadline { get; init; }
    public Guid CategoryId { get; init; }
    /// <summary>Optional updated list of eligible academic fields of study.</summary>
    public string[]? FieldsOfStudy { get; init; }

    /// <summary>
    /// Optional updated Review Service Fee. Null leaves the existing value
    /// untouched (so the legacy ConfigureReviewFee endpoint and existing
    /// PUT bodies both keep working); a non-negative value (0 = free)
    /// updates it.
    /// </summary>
    public decimal? ReviewFeeUsd { get; init; }

    /// <summary>
    /// Optional updated list of document names the applicant must upload.
    /// Null leaves the existing value untouched; an empty array clears it.
    /// </summary>
    public string[]? RequiredDocuments { get; init; }
}

public class UpdateScholarshipCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    : IRequestHandler<UpdateScholarshipCommand, bool>
{
    public async Task<bool> Handle(UpdateScholarshipCommand request, CancellationToken ct)
    {
        var entity = await db.Scholarships
            .Include(x => x.Applications)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity == null) throw new NotFoundException(nameof(Scholarship), request.Id);

        if (entity.OwnerScholarshipProviderId != user.UserId)
            throw new ForbiddenAccessException();

        if (entity.Applications.Any() && entity.CategoryId != request.CategoryId)
            throw new ConflictException("Cannot change scholarship category while applications are in progress.");

        entity.TitleEn = request.TitleEn;
        entity.TitleAr = request.TitleAr;
        entity.DescriptionEn = request.DescriptionEn;
        entity.DescriptionAr = request.DescriptionAr;
        entity.Deadline = request.Deadline;
        entity.CategoryId = request.CategoryId;
        entity.FieldsOfStudyJson = request.FieldsOfStudy is { Length: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(request.FieldsOfStudy)
            : null;

        // Only overwrite required documents when the caller sent the field.
        if (request.RequiredDocuments is not null)
            entity.RequiredDocumentsJson = CreateScholarshipCommandHandler.NormalizeRequiredDocs(request.RequiredDocuments);

        // PB-005: only overwrite the Review Service Fee when the caller actually
        // sent one — null means "leave the configured fee as-is" so the legacy
        // ConfigureReviewFee endpoint and existing PUT bodies keep working.
        // A fee of 0 marks the listing as free (no payment authorisation, no
        // commission); the Apply Now flow short-circuits for free listings.
        if (request.ReviewFeeUsd is { } fee)
        {
            if (fee < 0m)
                throw new ConflictException("Review Service Fee cannot be negative.");
            if (fee > 500m)
                throw new ConflictException("Review Service Fee cannot exceed $500.");

            // Master switch: when payments are disabled platform-wide, force
            // the fee to 0 silently regardless of what the ScholarshipProvider sent.
            var paymentsEnabled = await PlatformSettingsReader.GetBooleanAsync(
                db, PlatformSettingsKeys.PaymentsEnabled, defaultValue: true, ct);
            if (!paymentsEnabled)
            {
                entity.ReviewFeeUsd = 0m;
            }
            else
            {
                if (fee == 0m)
                {
                    var freeAllowed = await PlatformSettingsReader.GetBooleanAsync(
                        db, PlatformSettingsKeys.AllowFreeScholarships, defaultValue: true, ct);
                    if (!freeAllowed)
                        throw new ConflictException(
                            "Free in-app scholarships are not enabled on this platform. Please set a Review Service Fee greater than 0.");
                }
                entity.ReviewFeeUsd = fee;
            }
        }

        // PB-005: editing a live (Open) listing sends it back through moderation
        // so the changed content is re-reviewed by an admin before it is public
        // again. Editing a REJECTED draft resubmits it (clears the feedback and
        // re-enters the queue). Plain drafts / under-review / closed are untouched.
        if (entity.Status == ScholarshipStatus.Open)
        {
            entity.Status = ScholarshipStatus.UnderReview;
            entity.OpenedAt = null;
        }
        else if (entity.Status == ScholarshipStatus.Draft && entity.RejectionReason is not null)
        {
            entity.Status = ScholarshipStatus.UnderReview;
            entity.RejectionReason = null;
            entity.RejectedAt = null;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
