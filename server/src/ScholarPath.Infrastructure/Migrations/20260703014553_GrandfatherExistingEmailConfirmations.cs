using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <summary>
    /// FR-AUTH-05 — email verification is becoming REQUIRED before onboarding.
    /// Grandfather every EXISTING account as verified so the new blocking rule only
    /// affects new sign-ups; retroactively locking out live users would be wrong.
    /// New accounts created after this migration still register unverified and must
    /// confirm their email.
    /// </summary>
    public partial class GrandfatherExistingEmailConfirmations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Users] SET [EmailConfirmed] = 1 WHERE [EmailConfirmed] = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: we cannot know which accounts were originally unverified, and
            // un-confirming them would be harmful. Intentionally not reversed.
        }
    }
}
