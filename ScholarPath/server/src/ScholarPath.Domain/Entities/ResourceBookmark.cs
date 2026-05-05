using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class ResourceBookmark : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ResourceId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Resource Resource { get; set; } = null!;
}
