using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class UpgradeRequestLink : BaseEntity
{
    public Guid UpgradeRequestId { get; set; }
    public string Url { get; set; } = string.Empty;
    public LinkLabel Label { get; set; }

    // Navigation
    public UpgradeRequest UpgradeRequest { get; set; } = null!;
}
