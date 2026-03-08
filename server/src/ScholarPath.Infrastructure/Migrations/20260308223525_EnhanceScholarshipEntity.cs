using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceScholarshipEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentsChecklist",
                table: "Scholarships",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HowToApplyHtml",
                table: "Scholarships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverviewHtml",
                table: "Scholarships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "Scholarships",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderNameAr",
                table: "Scholarships",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Scholarships",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Published");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Scholarships",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Scholarships",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                columns: new[] { "DocumentsChecklist", "HowToApplyHtml", "OverviewHtml", "ProviderName", "ProviderNameAr", "Status", "Tags" },
                values: new object[] { null, null, null, "", null, "Published", null });

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                columns: new[] { "DocumentsChecklist", "HowToApplyHtml", "OverviewHtml", "ProviderName", "ProviderNameAr", "Status", "Tags" },
                values: new object[] { null, null, null, "", null, "Published", null });

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"),
                columns: new[] { "DocumentsChecklist", "HowToApplyHtml", "OverviewHtml", "ProviderName", "ProviderNameAr", "Status", "Tags" },
                values: new object[] { null, null, null, "", null, "Published", null });

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000004"),
                columns: new[] { "DocumentsChecklist", "HowToApplyHtml", "OverviewHtml", "ProviderName", "ProviderNameAr", "Status", "Tags" },
                values: new object[] { null, null, null, "", null, "Published", null });

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000005"),
                columns: new[] { "DocumentsChecklist", "HowToApplyHtml", "OverviewHtml", "ProviderName", "ProviderNameAr", "Status", "Tags" },
                values: new object[] { null, null, null, "", null, "Published", null });

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_Country_DegreeLevel",
                table: "Scholarships",
                columns: new[] { "Country", "DegreeLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_FieldOfStudy_Deadline",
                table: "Scholarships",
                columns: new[] { "FieldOfStudy", "Deadline" });

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_Status",
                table: "Scholarships",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_Status_IsDeleted",
                table: "Scholarships",
                columns: new[] { "Status", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scholarships_Country_DegreeLevel",
                table: "Scholarships");

            migrationBuilder.DropIndex(
                name: "IX_Scholarships_FieldOfStudy_Deadline",
                table: "Scholarships");

            migrationBuilder.DropIndex(
                name: "IX_Scholarships_Status",
                table: "Scholarships");

            migrationBuilder.DropIndex(
                name: "IX_Scholarships_Status_IsDeleted",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "DocumentsChecklist",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "HowToApplyHtml",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "OverviewHtml",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "ProviderNameAr",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Scholarships");
        }
    }
}
