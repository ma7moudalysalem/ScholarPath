using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

public class ChatConversation : AuditableEntity
{
    public Guid ParticipantOneId { get; set; }
    public Guid ParticipantTwoId { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public Guid? LastMessageId { get; set; }
    public bool IsArchivedForParticipantOne { get; set; }
    public bool IsArchivedForParticipantTwo { get; set; }

    public ICollection<ChatMessage> Messages { get; } = [];
}

public class ChatMessage : AuditableEntity, ISoftDeletable
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Body { get; set; } = default!;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ChatConversation? Conversation { get; set; }
}

public class UserBlock : BaseEntity
{
    public Guid BlockerId { get; set; }
    public Guid BlockedUserId { get; set; }
    public DateTimeOffset BlockedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
}
