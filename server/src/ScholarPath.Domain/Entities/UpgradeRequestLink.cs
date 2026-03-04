using System;
using System.Collections.Generic;
using System.Text;
using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class UpgradeRequestLink : BaseEntity
{
    // الربط مع الطلب الأساسي
    public Guid UpgradeRequestId { get; set; }

    // عنوان الرابط (مثل: LinkedIn, Portfolio, GitHub)
    public string Label { get; set; } = string.Empty;

    // الرابط الفعلي
    public string Url { get; set; } = string.Empty;
}
