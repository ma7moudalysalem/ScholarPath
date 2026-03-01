using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .HasMaxLength(5000)
            .IsRequired();

        builder.HasIndex(m => m.SenderId)
            .HasDatabaseName("IX_Messages_SenderId");

        builder.HasIndex(m => m.ReceiverId)
            .HasDatabaseName("IX_Messages_ReceiverId");

        builder.HasIndex(m => m.GroupId)
            .HasDatabaseName("IX_Messages_GroupId");

        builder.HasIndex(m => m.CreatedAt)
            .HasDatabaseName("IX_Messages_CreatedAt");

        // A message targets either a user (DM) or a group, never both or neither
        builder.ToTable(t => t.HasCheckConstraint("CK_Messages_ReceiverOrGroup",
            "(ReceiverId IS NOT NULL AND GroupId IS NULL) OR (ReceiverId IS NULL AND GroupId IS NOT NULL)"));

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId);

        builder.HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId);

        builder.HasOne(m => m.Group)
            .WithMany(g => g.Messages)
            .HasForeignKey(m => m.GroupId);
    }
}
