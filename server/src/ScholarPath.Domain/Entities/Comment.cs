using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class Comment : AuditableEntity, ISoftDeletable
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public Post Post { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
}
