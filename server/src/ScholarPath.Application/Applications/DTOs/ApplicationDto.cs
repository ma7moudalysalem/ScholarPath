using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Queries.GetApplications;

public sealed record ApplicationDto(
    Guid Id,
    Guid? ScholarshipId,
    string ScholarshipTitleEn,
    string ScholarshipTitleAr,
    string Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? Deadline,
    bool IsActive,
    bool IsReadOnly,
    string? PersonalNotes);

// Extension method في نفس الملف لسهولة الوصول (أو خليها static جوا الـ record)
public static class ApplicationMappingExtensions
{
    public static ApplicationDto ToDto(this ApplicationTracker entity)
    {
        // Free-text trackers (no platform link) carry their own title; surface
        // it in both language slots so the UI displays something sensible.
        var titleEn = entity.Scholarship?.TitleEn ?? entity.ExternalTitle ?? "N/A";
        var titleAr = entity.Scholarship?.TitleAr ?? entity.ExternalTitle ?? "غير محدد";

        return new ApplicationDto(
            Id: entity.Id,
            ScholarshipId: entity.ScholarshipId,
            ScholarshipTitleEn: titleEn,
            ScholarshipTitleAr: titleAr,
            Status: entity.Status.ToString(),
            SubmittedAt: entity.SubmittedAt,
            // Platform deadline first, free-text deadline as fallback.
            Deadline: entity.Scholarship?.Deadline ?? entity.Deadline,
            // منطق محمود للـ Kanban
            IsActive: entity.Status is not (ApplicationStatus.Withdrawn or ApplicationStatus.Rejected or ApplicationStatus.Accepted),
            IsReadOnly: entity.Status is ApplicationStatus.Accepted or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn,
            PersonalNotes: entity.PersonalNotes
        );
    }
}
