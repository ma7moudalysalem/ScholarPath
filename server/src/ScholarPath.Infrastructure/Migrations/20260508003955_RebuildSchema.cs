using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RebuildSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_Applications_StudentId_ScholarshipId'
                AND object_id = OBJECT_ID('Applications')
            )
            BEGIN
                DROP INDEX [IX_Applications_StudentId_ScholarshipId]
                ON [Applications];
            END
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'UX_Applications_Student_Scholarship_Active'
                AND object_id = OBJECT_ID('Applications')
            )
            BEGIN
                CREATE UNIQUE INDEX [UX_Applications_Student_Scholarship_Active]
                ON [Applications] ([StudentId], [ScholarshipId])
                WHERE [Status] IN ('Draft', 'Pending', 'UnderReview');
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'UX_Applications_Student_Scholarship_Active'
                AND object_id = OBJECT_ID('Applications')
            )
            BEGIN
                DROP INDEX [UX_Applications_Student_Scholarship_Active]
                ON [Applications];
            END
            """);
    }
}
