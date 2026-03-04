using System;
using System.Collections.Generic;
using System.Text;

using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class UpgradeRequestFile : BaseEntity
{
    // الربط مع الطلب الأساسي
    public Guid UpgradeRequestId { get; set; }

    // اسم الملف الأصلي (مثل: my_cv.pdf)
    public string FileName { get; set; } = string.Empty;

    // المسار المخزن فيه الملف على السيرفر (مثل: uploads/upgrades/guid.pdf)
    public string FilePath { get; set; } = string.Empty;

    // حجم الملف بالبايت
    public long FileSize { get; set; }

    // نوع الملف (مثل: application/pdf)
    public string ContentType { get; set; } = string.Empty;

    // تاريخ الرفع
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
