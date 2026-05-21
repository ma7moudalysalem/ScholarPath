using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <summary>
    /// Adds the extended Company and Consultant onboarding fields to UserProfiles so the
    /// admin reviewer sees a complete onboarding request (FR-ONB-03/04/05). All columns
    /// are nullable to preserve historical rows that pre-date the wider form.
    /// </summary>
    public partial class ExpandOnboardingProfileFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Company fields ──────────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "OrganizationEmail", table: "UserProfiles",
                type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "OrganizationCountry", table: "UserProfiles",
                type: "nvarchar(80)", maxLength: 80, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "OrganizationTaxNumber", table: "UserProfiles",
                type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CompanyType", table: "UserProfiles",
                type: "nvarchar(40)", maxLength: 40, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CompanyDescription", table: "UserProfiles",
                type: "nvarchar(1000)", maxLength: 1000, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "ContactPersonFullName", table: "UserProfiles",
                type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "ContactPersonPosition", table: "UserProfiles",
                type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "ContactPhoneNumber", table: "UserProfiles",
                type: "nvarchar(40)", maxLength: 40, nullable: true);

            // ── Consultant fields ───────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "ProfessionalTitle", table: "UserProfiles",
                type: "nvarchar(150)", maxLength: 150, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "HighestDegree", table: "UserProfiles",
                type: "nvarchar(150)", maxLength: 150, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "FieldOfExpertise", table: "UserProfiles",
                type: "nvarchar(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience", table: "UserProfiles",
                type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "PortfolioUrl", table: "UserProfiles",
                type: "nvarchar(2048)", maxLength: 2048, nullable: true);

            // Some of the pre-existing string columns had no MaxLength and were
            // emitted as nvarchar(max); widen-then-shrink to the new caps so the
            // schema matches the entity configuration moving forward.
            migrationBuilder.AlterColumn<string>(
                name: "OrganizationLegalName", table: "UserProfiles",
                type: "nvarchar(200)", maxLength: 200, nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(
                name: "OrganizationWebsite", table: "UserProfiles",
                type: "nvarchar(300)", maxLength: 300, nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
            migrationBuilder.AlterColumn<string>(
                name: "OrganizationRegistrationNumber", table: "UserProfiles",
                type: "nvarchar(100)", maxLength: 100, nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OrganizationEmail", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "OrganizationCountry", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "OrganizationTaxNumber", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "CompanyType", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "CompanyDescription", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "ContactPersonFullName", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "ContactPersonPosition", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "ContactPhoneNumber", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "ProfessionalTitle", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "HighestDegree", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "FieldOfExpertise", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "YearsOfExperience", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "PortfolioUrl", table: "UserProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationLegalName", table: "UserProfiles",
                type: "nvarchar(max)", nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(200)", oldMaxLength: 200, oldNullable: true);
            migrationBuilder.AlterColumn<string>(
                name: "OrganizationWebsite", table: "UserProfiles",
                type: "nvarchar(max)", nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(300)", oldMaxLength: 300, oldNullable: true);
            migrationBuilder.AlterColumn<string>(
                name: "OrganizationRegistrationNumber", table: "UserProfiles",
                type: "nvarchar(max)", nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100, oldNullable: true);
        }
    }
}
