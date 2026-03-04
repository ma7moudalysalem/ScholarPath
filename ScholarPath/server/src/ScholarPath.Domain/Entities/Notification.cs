using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageAr { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}
