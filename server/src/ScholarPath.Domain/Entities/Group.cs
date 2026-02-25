using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class Group : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public Guid CreatorId { get; set; }
    public bool IsPrivate { get; set; }
    public int MaxMembers { get; set; } = 100;
    public string? ImageUrl { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ApplicationUser Creator { get; set; } = null!;
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
