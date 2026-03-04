using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class Like : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Post? Post { get; set; }
    public Comment? Comment { get; set; }
}
