using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class UpgradeRequestFile : BaseEntity
{
    public Guid UpgradeRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public UpgradeRequest UpgradeRequest { get; set; } = null!;
}
