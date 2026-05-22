using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <summary>
    /// Auth alignment AUTH-CODE-03 + AUTH-CODE-06 — adds four conditional
    /// applicability fields and two rejection-feedback fields to UserProfiles:
    ///   IsTaxRegistered, TaxNotApplicableReason,
    ///   IsLegallyRegistered, LegalRegistrationNotApplicableReason,
    ///   LastOnboardingRejectionReason, LastOnboardingRejectedAt.
    ///
    /// Every statement is guarded with IF [NOT] EXISTS so the migration is
    /// safe to re-run on a database at any partial state — matching the
    /// idempotency policy established by the preceding migration.
    /// </summary>
    public partial class AddOnboardingApplicabilityFields_AuthCode03 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IsTaxRegistered' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [IsTaxRegistered] bit NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TaxNotApplicableReason' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [TaxNotApplicableReason] nvarchar(500) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IsLegallyRegistered' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [IsLegallyRegistered] bit NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LegalRegistrationNotApplicableReason' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [LegalRegistrationNotApplicableReason] nvarchar(500) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LastOnboardingRejectionReason' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [LastOnboardingRejectionReason] nvarchar(2000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LastOnboardingRejectedAt' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [LastOnboardingRejectedAt] datetimeoffset NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var col in new[]
            {
                "IsTaxRegistered","TaxNotApplicableReason",
                "IsLegallyRegistered","LegalRegistrationNotApplicableReason",
                "LastOnboardingRejectionReason","LastOnboardingRejectedAt"
            })
            {
                migrationBuilder.Sql(
                    $@"IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'{col}' AND Object_ID = Object_ID(N'UserProfiles'))
                       ALTER TABLE [UserProfiles] DROP COLUMN [{col}];");
            }
        }
    }
}
