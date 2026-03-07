using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantCompanyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExperienceSummary",
                table: "UpgradeRequests",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPhone",
                table: "UpgradeRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPersonName",
                table: "UpgradeRequests",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "UpgradeRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyRegistrationNumber",
                table: "UpgradeRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReasons",
                table: "UpgradeRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedById",
                table: "UpgradeRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EducationEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpgradeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DegreeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldOfStudy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartYear = table.Column<int>(type: "int", nullable: false),
                    EndYear = table.Column<int>(type: "int", nullable: true),
                    IsCurrentlyStudying = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationEntries_UpgradeRequests_UpgradeRequestId",
                        column: x => x.UpgradeRequestId,
                        principalTable: "UpgradeRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExpertiseTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertiseTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeRequestFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpgradeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeRequestFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeRequestFiles_UpgradeRequests_UpgradeRequestId",
                        column: x => x.UpgradeRequestId,
                        principalTable: "UpgradeRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UpgradeRequestLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpgradeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeRequestLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeRequestLinks_UpgradeRequests_UpgradeRequestId",
                        column: x => x.UpgradeRequestId,
                        principalTable: "UpgradeRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UpgradeRequestExpertiseTag",
                columns: table => new
                {
                    ExpertiseTagsListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpgradeRequestsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeRequestExpertiseTag", x => new { x.ExpertiseTagsListId, x.UpgradeRequestsId });
                    table.ForeignKey(
                        name: "FK_UpgradeRequestExpertiseTag_ExpertiseTags_ExpertiseTagsListId",
                        column: x => x.ExpertiseTagsListId,
                        principalTable: "ExpertiseTags",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UpgradeRequestExpertiseTag_UpgradeRequests_UpgradeRequestsId",
                        column: x => x.UpgradeRequestsId,
                        principalTable: "UpgradeRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationEntries_UpgradeRequestId",
                table: "EducationEntries",
                column: "UpgradeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertiseTags_Name",
                table: "ExpertiseTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequestExpertiseTag_UpgradeRequestsId",
                table: "UpgradeRequestExpertiseTag",
                column: "UpgradeRequestsId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequestFiles_UpgradeRequestId",
                table: "UpgradeRequestFiles",
                column: "UpgradeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequestLinks_UpgradeRequestId",
                table: "UpgradeRequestLinks",
                column: "UpgradeRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationEntries");

            migrationBuilder.DropTable(
                name: "UpgradeRequestExpertiseTag");

            migrationBuilder.DropTable(
                name: "UpgradeRequestFiles");

            migrationBuilder.DropTable(
                name: "UpgradeRequestLinks");

            migrationBuilder.DropTable(
                name: "ExpertiseTags");

            migrationBuilder.DropColumn(
                name: "RejectionReasons",
                table: "UpgradeRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "UpgradeRequests");

            migrationBuilder.AlterColumn<string>(
                name: "ExperienceSummary",
                table: "UpgradeRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1500)",
                oldMaxLength: 1500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPhone",
                table: "UpgradeRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactPersonName",
                table: "UpgradeRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContactEmail",
                table: "UpgradeRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyRegistrationNumber",
                table: "UpgradeRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);
        }
    }
}
