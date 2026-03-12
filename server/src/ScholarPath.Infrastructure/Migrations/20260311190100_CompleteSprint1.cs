using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteSprint1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Resources");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHtml",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHtmlAr",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excerpt",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcerptAr",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadingTimeMinutes",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Topic",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ResourceBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceBookmarks_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedItemIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceProgresses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceProgresses_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ExpertiseTags",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("80000000-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Essay Writing", null },
                    { new Guid("80000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Interview Prep", null },
                    { new Guid("80000000-0000-0000-0000-000000000003"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Visa Application", null },
                    { new Guid("80000000-0000-0000-0000-000000000004"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "University Selection", null },
                    { new Guid("80000000-0000-0000-0000-000000000005"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Career Counseling", null },
                    { new Guid("80000000-0000-0000-0000-000000000006"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Portfolio Review", null }
                });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000001"),
                columns: new[] { "AttachmentsJson", "ContentHtml", "ContentHtmlAr", "DifficultyLevel", "Excerpt", "ExcerptAr", "ReadingTimeMinutes", "Status", "Topic", "ViewCount" },
                values: new object[] { null, null, null, null, null, null, 0, 1, 0, 0 });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000002"),
                columns: new[] { "AttachmentsJson", "ContentHtml", "ContentHtmlAr", "DifficultyLevel", "Excerpt", "ExcerptAr", "ReadingTimeMinutes", "Status", "Topic", "ViewCount" },
                values: new object[] { null, null, null, null, null, null, 0, 1, 0, 0 });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000003"),
                columns: new[] { "AttachmentsJson", "ContentHtml", "ContentHtmlAr", "DifficultyLevel", "Excerpt", "ExcerptAr", "ReadingTimeMinutes", "Status", "Topic", "ViewCount" },
                values: new object[] { null, null, null, null, null, null, 0, 1, 0, 0 });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000004"),
                columns: new[] { "AttachmentsJson", "ContentHtml", "ContentHtmlAr", "DifficultyLevel", "Excerpt", "ExcerptAr", "ReadingTimeMinutes", "Status", "Topic", "ViewCount" },
                values: new object[] { null, null, null, null, null, null, 0, 1, 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBookmarks_ResourceId",
                table: "ResourceBookmarks",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBookmarks_UserId",
                table: "ResourceBookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProgresses_ResourceId",
                table: "ResourceProgresses",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProgresses_UserId",
                table: "ResourceProgresses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceBookmarks");

            migrationBuilder.DropTable(
                name: "ResourceProgresses");

            migrationBuilder.DeleteData(
                table: "ExpertiseTags",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "ExpertiseTags",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "ExpertiseTags",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "ExpertiseTags",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "ExpertiseTags",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "ExpertiseTags",
                keyColumn: "Id",
                keyValue: new Guid("80000000-0000-0000-0000-000000000006"));

            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ContentHtml",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ContentHtmlAr",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Excerpt",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ExcerptAr",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ReadingTimeMinutes",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Resources");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Resources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Resources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Resources",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000001"),
                columns: new[] { "Category", "Type", "Url" },
                values: new object[] { "Application Tips", "Article", "https://scholarpath.com/resources/scholarship-essay-guide" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000002"),
                columns: new[] { "Category", "Type", "Url" },
                values: new object[] { "Test Preparation", "Guide", "https://scholarpath.com/resources/ielts-study-plan" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000003"),
                columns: new[] { "Category", "Type", "Url" },
                values: new object[] { "Scholarship Search", "Article", "https://scholarpath.com/resources/scholarship-databases" });

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000004"),
                columns: new[] { "Category", "Type", "Url" },
                values: new object[] { "Application Tips", "Template", "https://scholarpath.com/resources/recommendation-letter-template" });
        }
    }
}
