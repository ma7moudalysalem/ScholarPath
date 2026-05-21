using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <summary>
    /// Adds 13 onboarding columns to UserProfiles (Company + Consultant fields)
    /// and 3 purely-external application fields to Applications, plus relaxes
    /// the Applications.ScholarshipId column to nullable so external apps can
    /// exist without a system scholarship link.
    ///
    /// The Up body was hand-written: `dotnet ef migrations add` produced an
    /// empty Up because the model snapshot was already in sync with the
    /// entities (left there by earlier deleted migrations), so EF detected
    /// no schema delta to emit. All statements are guarded with
    /// IF [NOT] EXISTS so re-running on a DB at any partial state is safe.
    /// </summary>
    public partial class ExpandUserProfileAndApplications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // UserProfiles: 13 new onboarding columns
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationEmail' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationEmail] nvarchar(256) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationCountry' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationCountry] nvarchar(80) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationTaxNumber' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationTaxNumber] nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompanyType' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [CompanyType] nvarchar(40) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompanyDescription' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [CompanyDescription] nvarchar(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPersonFullName' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPersonFullName] nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPersonPosition' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPersonPosition] nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPhoneNumber' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPhoneNumber] nvarchar(40) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ProfessionalTitle' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ProfessionalTitle] nvarchar(150) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'HighestDegree' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [HighestDegree] nvarchar(150) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'FieldOfExpertise' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [FieldOfExpertise] nvarchar(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'YearsOfExperience' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [YearsOfExperience] int NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'PortfolioUrl' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [PortfolioUrl] nvarchar(2048) NULL;
");

            // Applications: 3 new purely-external fields
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ExternalTitle' AND Object_ID = Object_ID(N'Applications'))
    ALTER TABLE [Applications] ADD [ExternalTitle] nvarchar(500) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ExternalProvider' AND Object_ID = Object_ID(N'Applications'))
    ALTER TABLE [Applications] ADD [ExternalProvider] nvarchar(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'Deadline' AND Object_ID = Object_ID(N'Applications'))
    ALTER TABLE [Applications] ADD [Deadline] datetimeoffset NULL;
");

            // Applications.ScholarshipId -> nullable (drop old FK + unique index, recreate filtered)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Applications_Student_Scholarship_Active' AND object_id = OBJECT_ID(N'Applications'))
    DROP INDEX [UX_Applications_Student_Scholarship_Active] ON [Applications];

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'ScholarshipId' AND Object_ID = Object_ID(N'Applications') AND is_nullable = 0
)
BEGIN
    DECLARE @fk_name SYSNAME;
    SELECT @fk_name = name FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'Applications')
          AND OBJECT_NAME(referenced_object_id) = N'Scholarships';
    IF @fk_name IS NOT NULL EXEC('ALTER TABLE [Applications] DROP CONSTRAINT [' + @fk_name + ']');

    ALTER TABLE [Applications] ALTER COLUMN [ScholarshipId] uniqueidentifier NULL;

    ALTER TABLE [Applications] ADD CONSTRAINT [FK_Applications_Scholarships_ScholarshipId]
        FOREIGN KEY ([ScholarshipId]) REFERENCES [Scholarships]([Id]) ON DELETE NO ACTION;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var col in new[]
            {
                "OrganizationEmail","OrganizationCountry","OrganizationTaxNumber",
                "CompanyType","CompanyDescription","ContactPersonFullName",
                "ContactPersonPosition","ContactPhoneNumber","ProfessionalTitle",
                "HighestDegree","FieldOfExpertise","YearsOfExperience","PortfolioUrl"
            })
            {
                migrationBuilder.Sql(
                    $@"IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'{col}' AND Object_ID = Object_ID(N'UserProfiles'))
                       ALTER TABLE [UserProfiles] DROP COLUMN [{col}];");
            }
            foreach (var col in new[] { "ExternalTitle", "ExternalProvider", "Deadline" })
            {
                migrationBuilder.Sql(
                    $@"IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'{col}' AND Object_ID = Object_ID(N'Applications'))
                       ALTER TABLE [Applications] DROP COLUMN [{col}];");
            }
        }
    }
}
