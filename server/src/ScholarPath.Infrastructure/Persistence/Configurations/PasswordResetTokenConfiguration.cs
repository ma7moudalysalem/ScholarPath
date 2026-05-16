using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId);
    }
}
