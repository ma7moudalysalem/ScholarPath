using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsEntities_PB017 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiRedactionAuditSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiInteractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RedactedPrompt = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    SampledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Verdict = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRedactionAuditSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiRedactionAuditSamples_AiInteractions_AiInteractionId",
                        column: x => x.AiInteractionId,
                        principalTable: "AiInteractions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiRedactionAuditSamples_Users_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiRedactionAuditSamples_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationClickEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScholarshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AiInteractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClickedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationClickEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationClickEvents_AiInteractions_AiInteractionId",
                        column: x => x.AiInteractionId,
                        principalTable: "AiInteractions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RecommendationClickEvents_Scholarships_ScholarshipId",
                        column: x => x.ScholarshipId,
                        principalTable: "Scholarships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationClickEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiRedactionAuditSamples_AiInteractionId",
                table: "AiRedactionAuditSamples",
                column: "AiInteractionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiRedactionAuditSamples_ReviewerUserId",
                table: "AiRedactionAuditSamples",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiRedactionAuditSamples_SampledAt",
                table: "AiRedactionAuditSamples",
                column: "SampledAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiRedactionAuditSamples_UserId",
                table: "AiRedactionAuditSamples",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiRedactionAuditSamples_Verdict_SampledAt",
                table: "AiRedactionAuditSamples",
                columns: new[] { "Verdict", "SampledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationClickEvents_AiInteractionId",
                table: "RecommendationClickEvents",
                column: "AiInteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationClickEvents_ScholarshipId_ClickedAt",
                table: "RecommendationClickEvents",
                columns: new[] { "ScholarshipId", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationClickEvents_UserId_ClickedAt",
                table: "RecommendationClickEvents",
                columns: new[] { "UserId", "ClickedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiRedactionAuditSamples");

            migrationBuilder.DropTable(
                name: "RecommendationClickEvents");
        }
    }
}
