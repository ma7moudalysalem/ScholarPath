using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class ResourceCompletedItem : BaseEntity
{
    public Guid ResourceProgressId { get; set; }
    public string ItemId { get; set; } = string.Empty;

    public ResourceProgress ResourceProgress { get; set; } = null!;
}
