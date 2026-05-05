using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class ScholarshipTag : BaseEntity
{
    public Guid ScholarshipId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Scholarship Scholarship { get; set; } = null!;
}

public class ScholarshipEligibleCountry : BaseEntity
{
    public Guid ScholarshipId { get; set; }
    public string CountryCode { get; set; } = string.Empty;

    public Scholarship Scholarship { get; set; } = null!;
}

public class ScholarshipEligibleMajor : BaseEntity
{
    public Guid ScholarshipId { get; set; }
    public string MajorName { get; set; } = string.Empty;

    public Scholarship Scholarship { get; set; } = null!;
}

public class ScholarshipDocumentChecklist : BaseEntity
{
    public Guid ScholarshipId { get; set; }
    public string DocumentName { get; set; } = string.Empty;

    public Scholarship Scholarship { get; set; } = null!;
}
