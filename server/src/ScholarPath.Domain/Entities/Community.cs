using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class ForumCategory : AuditableEntity
{
    public string NameEn { get; set; } = default!;
    public string NameAr { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ForumPost> Posts { get; } = [];
}

public class ForumPost : AuditableEntity, ISoftDeletable
{
    public Guid AuthorId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ParentPostId { get; set; } // null = root thread; non-null = reply

    public string? Title { get; set; } // only on root
    public string BodyMarkdown { get; set; } = default!;
    public PostModerationStatus ModerationStatus { get; set; } = PostModerationStatus.Visible;

    // Cached aggregates
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int FlagCount { get; set; }
    public int ReplyCount { get; set; }

    public bool IsAutoHidden { get; set; } // flagged by threshold rule
    public DateTimeOffset? AutoHiddenAt { get; set; }
    public Guid? ModeratedByAdminId { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }
    public string? ModerationNote { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationUser? Author { get; set; }
    public ForumCategory? Category { get; set; }
    public ForumPost? ParentPost { get; set; }
    public ICollection<ForumPost> Replies { get; } = [];
    public ICollection<ForumPostAttachment> Attachments { get; } = [];
    public ICollection<ForumVote> Votes { get; } = [];
    public ICollection<ForumFlag> Flags { get; } = [];
}

public class ForumPostAttachment : BaseEntity
{
    public Guid ForumPostId { get; set; }
    public string FileName { get; set; } = default!;
    public string BlobUrl { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
}

public class ForumVote : BaseEntity
{
    public Guid ForumPostId { get; set; }
    public Guid UserId { get; set; }
    public VoteType VoteType { get; set; }
    public DateTimeOffset VotedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ForumFlag : BaseEntity
{
    public Guid ForumPostId { get; set; }
    public Guid FlaggedByUserId { get; set; }
    public string Reason { get; set; } = default!;
    public string? AdditionalDetails { get; set; }
    public DateTimeOffset FlaggedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsValid { get; set; } = true; // admin can invalidate
}
