using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class ExpertiseTag : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<UpgradeRequest> UpgradeRequests { get; set; } = new List<UpgradeRequest>();
}
