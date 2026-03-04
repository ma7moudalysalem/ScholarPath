using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(g => g.NameAr)
            .HasMaxLength(200);

        builder.Property(g => g.Description)
            .HasMaxLength(1000);

        builder.Property(g => g.DescriptionAr)
            .HasMaxLength(1000);

        builder.Property(g => g.ImageUrl)
            .HasMaxLength(500);

        builder.HasIndex(g => g.Name)
            .HasDatabaseName("IX_Groups_Name");

        builder.HasOne(g => g.Creator)
            .WithMany()
            .HasForeignKey(g => g.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Members)
            .WithOne(gm => gm.Group)
            .HasForeignKey(gm => gm.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Posts)
            .WithOne(p => p.Group)
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.HasKey(gm => gm.Id);

        builder.Property(gm => gm.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(gm => new { gm.UserId, gm.GroupId })
            .IsUnique()
            .HasDatabaseName("IX_GroupMembers_UserId_GroupId");

        builder.HasOne(gm => gm.User)
            .WithMany()
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
