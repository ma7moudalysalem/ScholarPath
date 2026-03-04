using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Content)
            .HasMaxLength(10000)
            .IsRequired();

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.HasIndex(p => p.GroupId)
            .HasDatabaseName("IX_Posts_GroupId");

        builder.HasIndex(p => p.AuthorId)
            .HasDatabaseName("IX_Posts_AuthorId");

        builder.HasIndex(p => new { p.GroupId, p.CreatedAt })
            .HasDatabaseName("IX_Posts_GroupId_CreatedAt");

        builder.HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Likes)
            .WithOne(l => l.Post)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(c => c.PostId)
            .HasDatabaseName("IX_Comments_PostId");

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing relationship for replies
        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Likes)
            .WithOne(l => l.Comment)
            .HasForeignKey(l => l.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.HasKey(l => l.Id);

        // Unique constraint: one like per user per post
        builder.HasIndex(l => new { l.UserId, l.PostId })
            .IsUnique()
            .HasFilter("PostId IS NOT NULL")
            .HasDatabaseName("IX_Likes_UserId_PostId");

        // Unique constraint: one like per user per comment
        builder.HasIndex(l => new { l.UserId, l.CommentId })
            .IsUnique()
            .HasFilter("CommentId IS NOT NULL")
            .HasDatabaseName("IX_Likes_UserId_CommentId");

        // Ensure a like targets either a post or a comment, never both or neither
        builder.ToTable(t => t.HasCheckConstraint("CK_Likes_PostOrComment",
            "(PostId IS NOT NULL AND CommentId IS NULL) OR (PostId IS NULL AND CommentId IS NOT NULL)"));

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
