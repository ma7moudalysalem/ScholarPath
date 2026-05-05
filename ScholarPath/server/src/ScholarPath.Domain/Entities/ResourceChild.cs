using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class ResourceAttachment : BaseEntity
{
    public Guid ResourceId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;

    public Resource Resource { get; set; } = null!;
}
