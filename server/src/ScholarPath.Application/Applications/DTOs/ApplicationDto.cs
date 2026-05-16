using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Queries.GetApplications;

public sealed record ApplicationDto(
    Guid Id,
    Guid ScholarshipId,
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
        return new ApplicationDto(
            Id: entity.Id,
            ScholarshipId: entity.ScholarshipId,
            ScholarshipTitleEn: entity.Scholarship?.TitleEn ?? "N/A",
            ScholarshipTitleAr: entity.Scholarship?.TitleAr ?? "غير محدد",
            Status: entity.Status.ToString(),
            SubmittedAt: entity.SubmittedAt,
            Deadline: entity.Scholarship?.Deadline,
            // منطق محمود للـ Kanban
            IsActive: entity.Status is not (ApplicationStatus.Withdrawn or ApplicationStatus.Rejected or ApplicationStatus.Accepted),
            IsReadOnly: entity.Status is ApplicationStatus.Accepted or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn,
            PersonalNotes: entity.PersonalNotes
        );
    }
}
