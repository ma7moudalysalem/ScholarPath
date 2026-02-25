using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class Category : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<Scholarship> Scholarships { get; set; } = new List<Scholarship>();
}
