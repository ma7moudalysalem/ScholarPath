using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class Message : BaseEntity, ISoftDeletable
{
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public Guid? GroupId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ApplicationUser Sender { get; set; } = null!;
    public ApplicationUser? Receiver { get; set; }
    public Group? Group { get; set; }
}
