using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBilingualForumPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyAr",
                table: "ForumPosts",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyEn",
                table: "ForumPosts",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleAr",
                table: "ForumPosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "ForumPosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Backfill existing rows onto the English side so the bilingual
            // display (which reads *En with a fallback) shows legacy content.
            // The Arabic side stays null and simply falls back to English.
            migrationBuilder.Sql(
                "UPDATE ForumPosts SET BodyEn = BodyMarkdown WHERE BodyEn IS NULL;");
            migrationBuilder.Sql(
                "UPDATE ForumPosts SET TitleEn = Title WHERE TitleEn IS NULL AND Title IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyAr",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "BodyEn",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "TitleAr",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "ForumPosts");
        }
    }
}
