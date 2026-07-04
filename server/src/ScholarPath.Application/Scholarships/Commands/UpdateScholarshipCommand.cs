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
    /// <summary>
    /// Updated target country. Null leaves the existing value untouched (keeps
    /// legacy PUT bodies working); a non-empty value overwrites it.
    /// </summary>
    public string? Country { get; init; }
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
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity == null) throw new NotFoundException(nameof(Scholarship), request.Id);

        // FR-SCH-35: a Closed or Archived listing is terminal — it can't be
        // edited in place (a Closed one must be reopened first; an Archived one
        // is soft-deleted). Only Draft / UnderReview / Open are editable.
        if (entity.Status is ScholarshipStatus.Closed or ScholarshipStatus.Archived)
            throw new ConflictException(
                "This scholarship can no longer be edited. Reopen a closed listing before editing it.");

        // FR-SCH-18/19: a provider may edit only their OWN listing. An
        // admin-created (ownerless) listing — e.g. an External scholarship — is
        // editable only by an Admin/SuperAdmin. Providers still cannot touch
        // another provider's listing.
        var isAdmin = user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
        if (entity.OwnerScholarshipProviderId is { } ownerId)
        {
            if (ownerId != user.UserId)
                throw new ForbiddenAccessException();
        }
        else if (!isAdmin)
        {
            throw new ForbiddenAccessException();
        }

        if (entity.Applications.Any() && entity.CategoryId != request.CategoryId)
            throw new ConflictException("Cannot change scholarship category while applications are in progress.");

        entity.TitleEn = request.TitleEn;
        entity.TitleAr = request.TitleAr;
        entity.DescriptionEn = request.DescriptionEn;
        entity.DescriptionAr = request.DescriptionAr;
        entity.Deadline = request.Deadline;
        entity.CategoryId = request.CategoryId;
        // Only overwrite the country when the caller actually sent one.
        if (!string.IsNullOrWhiteSpace(request.Country))
            entity.TargetCountriesJson = System.Text.Json.JsonSerializer.Serialize(new[] { request.Country.Trim() });
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

        // PB-005: editing a live (Open) PROVIDER listing sends it back through
        // moderation so the changed content is re-reviewed before it is public
        // again. Admin-created (ownerless) listings skip this — the admin IS the
        // moderator, so their edit stays live. Editing a REJECTED draft resubmits
        // it (clears the feedback and re-enters the queue). Plain drafts /
        // under-review / closed are untouched.
        if (entity.OwnerScholarshipProviderId is not null && entity.Status == ScholarshipStatus.Open)
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
