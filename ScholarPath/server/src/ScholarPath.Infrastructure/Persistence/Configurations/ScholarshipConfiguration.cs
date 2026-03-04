using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence.Configurations;

public class ScholarshipConfiguration : IEntityTypeConfiguration<Scholarship>
{
    public void Configure(EntityTypeBuilder<Scholarship> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(s => s.TitleAr)
            .HasMaxLength(300);

        builder.Property(s => s.Description)
            .IsRequired();

        builder.Property(s => s.Country)
            .HasMaxLength(100);

        builder.Property(s => s.FieldOfStudy)
            .HasMaxLength(200);

        builder.Property(s => s.FundingType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.DegreeLevel)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.AwardAmount)
            .HasPrecision(18, 2);

        builder.Property(s => s.Currency)
            .HasMaxLength(10);

        builder.Property(s => s.EligibilityDescription)
            .HasMaxLength(2000);

        builder.Property(s => s.RequiredDocuments)
            .HasMaxLength(2000);

        builder.Property(s => s.OfficialLink)
            .HasMaxLength(500);

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(500);

        builder.Property(s => s.MinGPA)
            .HasPrecision(4, 2);

        builder.Property(s => s.EligibleCountries)
            .HasMaxLength(2000);

        builder.Property(s => s.EligibleMajors)
            .HasMaxLength(2000);

        // Indexes
        builder.HasIndex(s => s.Deadline)
            .HasDatabaseName("IX_Scholarships_Deadline");

        builder.HasIndex(s => s.Country)
            .HasDatabaseName("IX_Scholarships_Country");

        builder.HasIndex(s => s.FieldOfStudy)
            .HasDatabaseName("IX_Scholarships_FieldOfStudy");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_Scholarships_IsActive");

        builder.HasIndex(s => s.DegreeLevel)
            .HasDatabaseName("IX_Scholarships_DegreeLevel");

        builder.HasIndex(s => s.FundingType)
            .HasDatabaseName("IX_Scholarships_FundingType");

        // Relationships
        builder.HasOne(s => s.Category)
            .WithMany(c => c.Scholarships)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.SavedScholarships)
            .WithOne(ss => ss.Scholarship)
            .HasForeignKey(ss => ss.ScholarshipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.NameAr)
            .HasMaxLength(150);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.DescriptionAr)
            .HasMaxLength(500);

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Categories_Name");
    }
}

public class SavedScholarshipConfiguration : IEntityTypeConfiguration<SavedScholarship>
{
    public void Configure(EntityTypeBuilder<SavedScholarship> builder)
    {
        builder.HasKey(ss => ss.Id);

        builder.HasIndex(ss => new { ss.UserId, ss.ScholarshipId })
            .IsUnique()
            .HasDatabaseName("IX_SavedScholarships_UserId_ScholarshipId");
    }
}
