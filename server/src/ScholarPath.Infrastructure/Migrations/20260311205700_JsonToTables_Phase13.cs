using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JsonToTables_Phase13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentsChecklist",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "EligibleCountries",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "EligibleMajors",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Scholarships");

            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "CompletedItemIdsJson",
                table: "ResourceProgresses");

            migrationBuilder.DropColumn(
                name: "ChecklistJson",
                table: "ApplicationTrackers");

            migrationBuilder.DropColumn(
                name: "RemindersJson",
                table: "ApplicationTrackers");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinGPA",
                table: "Scholarships",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldPrecision: 4,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationTrackerChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationTrackerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsChecked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationTrackerChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationTrackerChecklistItems_ApplicationTrackers_ApplicationTrackerId",
                        column: x => x.ApplicationTrackerId,
                        principalTable: "ApplicationTrackers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationTrackerReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationTrackerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReminderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationTrackerReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationTrackerReminders_ApplicationTrackers_ApplicationTrackerId",
                        column: x => x.ApplicationTrackerId,
                        principalTable: "ApplicationTrackers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceAttachments_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceCompletedItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceProgressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCompletedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceCompletedItems_ResourceProgresses_ResourceProgressId",
                        column: x => x.ResourceProgressId,
                        principalTable: "ResourceProgresses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScholarshipDocumentChecklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScholarshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScholarshipDocumentChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScholarshipDocumentChecklists_Scholarships_ScholarshipId",
                        column: x => x.ScholarshipId,
                        principalTable: "Scholarships",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScholarshipEligibleCountries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScholarshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScholarshipEligibleCountries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScholarshipEligibleCountries_Scholarships_ScholarshipId",
                        column: x => x.ScholarshipId,
                        principalTable: "Scholarships",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScholarshipEligibleMajors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScholarshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MajorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScholarshipEligibleMajors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScholarshipEligibleMajors_Scholarships_ScholarshipId",
                        column: x => x.ScholarshipId,
                        principalTable: "Scholarships",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScholarshipTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScholarshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScholarshipTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScholarshipTags_Scholarships_ScholarshipId",
                        column: x => x.ScholarshipId,
                        principalTable: "Scholarships",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ScholarshipEligibleCountries",
                columns: new[] { "Id", "CountryCode", "CreatedAt", "ScholarshipId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("31000000-0000-0000-0001-000000000001"), "Egypt", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("31000000-0000-0000-0001-000000000002"), "Jordan", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("31000000-0000-0000-0001-000000000003"), "Morocco", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("31000000-0000-0000-0001-000000000004"), "Tunisia", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("31000000-0000-0000-0001-000000000005"), "Lebanon", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("31000000-0000-0000-0002-000000000001"), "Egypt", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000002"), null },
                    { new Guid("31000000-0000-0000-0002-000000000002"), "Jordan", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000002"), null },
                    { new Guid("31000000-0000-0000-0002-000000000003"), "Iraq", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000002"), null },
                    { new Guid("31000000-0000-0000-0002-000000000004"), "Saudi Arabia", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000002"), null },
                    { new Guid("31000000-0000-0000-0002-000000000005"), "UAE", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000002"), null },
                    { new Guid("31000000-0000-0000-0003-000000000001"), "Egypt", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000003"), null },
                    { new Guid("31000000-0000-0000-0003-000000000002"), "Palestine", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000003"), null },
                    { new Guid("31000000-0000-0000-0003-000000000003"), "Syria", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000003"), null },
                    { new Guid("31000000-0000-0000-0003-000000000004"), "Yemen", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000003"), null },
                    { new Guid("31000000-0000-0000-0003-000000000005"), "Somalia", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000003"), null },
                    { new Guid("31000000-0000-0000-0004-000000000001"), "Egypt", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("31000000-0000-0000-0004-000000000002"), "Lebanon", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("31000000-0000-0000-0004-000000000003"), "Jordan", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("31000000-0000-0000-0004-000000000004"), "Morocco", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("31000000-0000-0000-0004-000000000005"), "Tunisia", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("31000000-0000-0000-0004-000000000006"), "Iraq", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("31000000-0000-0000-0005-000000000001"), "Egypt", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000005"), null },
                    { new Guid("31000000-0000-0000-0005-000000000002"), "Algeria", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000005"), null },
                    { new Guid("31000000-0000-0000-0005-000000000003"), "Libya", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000005"), null },
                    { new Guid("31000000-0000-0000-0005-000000000004"), "Sudan", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000005"), null },
                    { new Guid("31000000-0000-0000-0005-000000000005"), "Mauritania", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("30000000-0000-0000-0000-000000000005"), null }
                });

            migrationBuilder.InsertData(
                table: "ScholarshipEligibleMajors",
                columns: new[] { "Id", "CreatedAt", "MajorName", "ScholarshipId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("32000000-0000-0000-0001-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Computer Science", new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("32000000-0000-0000-0001-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Engineering", new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("32000000-0000-0000-0001-000000000003"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Natural Sciences", new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("32000000-0000-0000-0001-000000000004"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mathematics", new Guid("30000000-0000-0000-0000-000000000001"), null },
                    { new Guid("32000000-0000-0000-0004-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Public Policy", new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("32000000-0000-0000-0004-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Engineering", new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("32000000-0000-0000-0004-000000000003"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sciences", new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("32000000-0000-0000-0004-000000000004"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Education", new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("32000000-0000-0000-0004-000000000005"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Arts", new Guid("30000000-0000-0000-0000-000000000004"), null },
                    { new Guid("32000000-0000-0000-0005-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Environmental Science", new Guid("30000000-0000-0000-0000-000000000005"), null },
                    { new Guid("32000000-0000-0000-0005-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Public Health", new Guid("30000000-0000-0000-0000-000000000005"), null },
                    { new Guid("32000000-0000-0000-0005-000000000003"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Data Science", new Guid("30000000-0000-0000-0000-000000000005"), null },
                    { new Guid("32000000-0000-0000-0005-000000000004"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Urban Planning", new Guid("30000000-0000-0000-0000-000000000005"), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTrackerChecklistItems_ApplicationTrackerId",
                table: "ApplicationTrackerChecklistItems",
                column: "ApplicationTrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTrackerReminders_ApplicationTrackerId",
                table: "ApplicationTrackerReminders",
                column: "ApplicationTrackerId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAttachments_ResourceId",
                table: "ResourceAttachments",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCompletedItems_ResourceProgressId",
                table: "ResourceCompletedItems",
                column: "ResourceProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipDocumentChecklists_ScholarshipId",
                table: "ScholarshipDocumentChecklists",
                column: "ScholarshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipEligibleCountries_ScholarshipId",
                table: "ScholarshipEligibleCountries",
                column: "ScholarshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipEligibleMajors_ScholarshipId",
                table: "ScholarshipEligibleMajors",
                column: "ScholarshipId");

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipTags_ScholarshipId",
                table: "ScholarshipTags",
                column: "ScholarshipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationTrackerChecklistItems");

            migrationBuilder.DropTable(
                name: "ApplicationTrackerReminders");

            migrationBuilder.DropTable(
                name: "ResourceAttachments");

            migrationBuilder.DropTable(
                name: "ResourceCompletedItems");

            migrationBuilder.DropTable(
                name: "ScholarshipDocumentChecklists");

            migrationBuilder.DropTable(
                name: "ScholarshipEligibleCountries");

            migrationBuilder.DropTable(
                name: "ScholarshipEligibleMajors");

            migrationBuilder.DropTable(
                name: "ScholarshipTags");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinGPA",
                table: "Scholarships",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentsChecklist",
                table: "Scholarships",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EligibleCountries",
                table: "Scholarships",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EligibleMajors",
                table: "Scholarships",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Scholarships",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletedItemIdsJson",
                table: "ResourceProgresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChecklistJson",
                table: "ApplicationTrackers",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemindersJson",
                table: "ApplicationTrackers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000001"),
                column: "AttachmentsJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000002"),
                column: "AttachmentsJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000003"),
                column: "AttachmentsJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "Resources",
                keyColumn: "Id",
                keyValue: new Guid("50000000-0000-0000-0000-000000000004"),
                column: "AttachmentsJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                columns: new[] { "DocumentsChecklist", "EligibleCountries", "EligibleMajors", "Tags" },
                values: new object[] { null, "[\"Egypt\",\"Jordan\",\"Morocco\",\"Tunisia\",\"Lebanon\"]", "[\"Computer Science\",\"Engineering\",\"Natural Sciences\",\"Mathematics\"]", null });

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                columns: new[] { "DocumentsChecklist", "EligibleCountries", "EligibleMajors", "Tags" },
                values: new object[] { null, "[\"Egypt\",\"Jordan\",\"Iraq\",\"Saudi Arabia\",\"UAE\"]", null, null });

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"),
                columns: new[] { "DocumentsChecklist", "EligibleCountries", "EligibleMajors", "Tags" },
                values: new object[] { null, "[\"Egypt\",\"Palestine\",\"Syria\",\"Yemen\",\"Somalia\"]", null, null });

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000004"),
                columns: new[] { "DocumentsChecklist", "EligibleCountries", "EligibleMajors", "Tags" },
                values: new object[] { null, "[\"Egypt\",\"Lebanon\",\"Jordan\",\"Morocco\",\"Tunisia\",\"Iraq\"]", "[\"Public Policy\",\"Engineering\",\"Sciences\",\"Education\",\"Arts\"]", null });

            migrationBuilder.UpdateData(
                table: "Scholarships",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000005"),
                columns: new[] { "DocumentsChecklist", "EligibleCountries", "EligibleMajors", "Tags" },
                values: new object[] { null, "[\"Egypt\",\"Algeria\",\"Libya\",\"Sudan\",\"Mauritania\"]", "[\"Environmental Science\",\"Public Health\",\"Data Science\",\"Urban Planning\"]", null });
        }
    }
}
