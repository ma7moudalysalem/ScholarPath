using System;
using System.Collections.Generic;
using System.Text;
using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class UpgradeRequestLink : BaseEntity
{
    public Guid UpgradeRequestId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
