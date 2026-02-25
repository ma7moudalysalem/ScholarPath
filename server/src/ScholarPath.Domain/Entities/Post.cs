using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class Post : AuditableEntity, ISoftDeletable
{
    public Guid GroupId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
