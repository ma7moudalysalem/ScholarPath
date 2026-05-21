using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <summary>
    /// Adds support for "purely external" application trackers — entries the
    /// student creates from the Add External Application modal for scholarships
    /// that are NOT in the ScholarPath catalogue.
    ///
    /// Schema changes on the <c>Applications</c> table:
    ///   • <c>ScholarshipId</c> becomes NULLABLE (no platform link required).
    ///   • New free-text columns: <c>ExternalTitle</c>, <c>ExternalProvider</c>.
    ///   • New <c>Deadline</c> column (student-supplied, optional).
    ///   • The single-active-application filtered index is recreated to also
    ///     require <c>ScholarshipId IS NOT NULL</c> — so multiple parallel
    ///     external trackers don't trip the uniqueness rule.
    /// </summary>
    public partial class AddPurelyExternalApplicationFields : Migration
    {
        private const string ActiveAppIndex = "UX_Applications_Student_Scholarship_Active";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent — re-running the migration on a partially-migrated DB
            // is a no-op for the columns that already exist.
            migrationBuilder.Sql($@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Applications]')
                    AND name = 'ExternalTitle')
                BEGIN
                    ALTER TABLE [Applications]
                        ADD [ExternalTitle] NVARCHAR(300) NULL;
                END
            ");

            migrationBuilder.Sql($@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Applications]')
                    AND name = 'ExternalProvider')
                BEGIN
                    ALTER TABLE [Applications]
                        ADD [ExternalProvider] NVARCHAR(200) NULL;
                END
            ");

            migrationBuilder.Sql($@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Applications]')
                    AND name = 'Deadline')
                BEGIN
                    ALTER TABLE [Applications]
                        ADD [Deadline] DATETIMEOFFSET NULL;
                END
            ");

            // Make ScholarshipId nullable. The column already has values for every
            // existing row, so dropping NOT NULL is data-safe.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Applications]')
                    AND name = 'ScholarshipId'
                    AND is_nullable = 0)
                BEGIN
                    -- Drop the unique filtered index so we can alter the keyed column.
                    IF EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = '" + ActiveAppIndex + @"'
                        AND object_id = OBJECT_ID('Applications'))
                    BEGIN
                        DROP INDEX [" + ActiveAppIndex + @"] ON [Applications];
                    END;

                    ALTER TABLE [Applications]
                        ALTER COLUMN [ScholarshipId] UNIQUEIDENTIFIER NULL;
                END
            ");

            // Recreate the unique filtered index with the IS NOT NULL guard so
            // multiple ScholarshipId-less trackers can coexist for one student.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = '" + ActiveAppIndex + @"'
                    AND object_id = OBJECT_ID('Applications'))
                BEGIN
                    CREATE UNIQUE INDEX [" + ActiveAppIndex + @"]
                    ON [Applications] ([StudentId], [ScholarshipId])
                    WHERE [ScholarshipId] IS NOT NULL
                          AND [Status] <> 'Withdrawn'
                          AND [Status] <> 'Rejected'
                          AND [Status] <> 'Accepted';
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the recreated index first so we can re-tighten ScholarshipId.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = '" + ActiveAppIndex + @"'
                    AND object_id = OBJECT_ID('Applications'))
                BEGIN
                    DROP INDEX [" + ActiveAppIndex + @"] ON [Applications];
                END
            ");

            // NULL -> NOT NULL only succeeds if every row has a value. The Down
            // path is best-effort: callers who used the purely-external feature
            // will have to delete those rows before rolling back.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Applications]')
                    AND name = 'ScholarshipId'
                    AND is_nullable = 1)
                BEGIN
                    ALTER TABLE [Applications]
                        ALTER COLUMN [ScholarshipId] UNIQUEIDENTIFIER NOT NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = '" + ActiveAppIndex + @"'
                    AND object_id = OBJECT_ID('Applications'))
                BEGIN
                    CREATE UNIQUE INDEX [" + ActiveAppIndex + @"]
                    ON [Applications] ([StudentId], [ScholarshipId])
                    WHERE [Status] <> 'Withdrawn'
                          AND [Status] <> 'Rejected'
                          AND [Status] <> 'Accepted';
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Applications]')
                    AND name = 'Deadline')
                BEGIN
                    ALTER TABLE [Applications] DROP COLUMN [Deadline];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Applications]')
                    AND name = 'ExternalProvider')
                BEGIN
                    ALTER TABLE [Applications] DROP COLUMN [ExternalProvider];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[Applications]')
                    AND name = 'ExternalTitle')
                BEGIN
                    ALTER TABLE [Applications] DROP COLUMN [ExternalTitle];
                END
            ");
        }
    }
}
