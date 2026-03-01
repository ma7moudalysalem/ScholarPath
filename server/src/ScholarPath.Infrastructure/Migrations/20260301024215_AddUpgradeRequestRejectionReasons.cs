using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpgradeRequestRejectionReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SuccessStories_IsApproved",
                table: "SuccessStories");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReasons",
                table: "UpgradeRequests",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "RefreshTokens",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReasons",
                table: "UpgradeRequests");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessStories_IsApproved",
                table: "SuccessStories",
                column: "IsApproved");
        }
    }
}
