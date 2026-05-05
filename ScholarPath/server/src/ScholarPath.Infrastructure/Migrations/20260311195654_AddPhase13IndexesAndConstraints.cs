using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase13IndexesAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResourceProgresses_UserId",
                table: "ResourceProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ResourceBookmarks_UserId",
                table: "ResourceBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_IsDeleted",
                table: "Resources",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProgresses_UserId_ResourceId",
                table: "ResourceProgresses",
                columns: new[] { "UserId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBookmarks_UserId_ResourceId",
                table: "ResourceBookmarks",
                columns: new[] { "UserId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_IsDeleted",
                table: "Posts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId",
                table: "Likes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_IsDeleted",
                table: "Groups",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_IsDeleted",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_ResourceProgresses_UserId_ResourceId",
                table: "ResourceProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ResourceBookmarks_UserId_ResourceId",
                table: "ResourceBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Posts_IsDeleted",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Likes_UserId",
                table: "Likes");

            migrationBuilder.DropIndex(
                name: "IX_Groups_IsDeleted",
                table: "Groups");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProgresses_UserId",
                table: "ResourceProgresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBookmarks_UserId",
                table: "ResourceBookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }
    }
}
