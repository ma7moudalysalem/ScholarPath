using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantRatingSnapshotAndPenalty_PB006R : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConsultantAverageRating",
                table: "UserProfiles",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConsultantLowRatingFlaggedAt",
                table: "UserProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsultantRatingPenaltyFactor",
                table: "UserProfiles",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<int>(
                name: "ConsultantReviewCount",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ConsultantLowRatingFlagged",
                table: "UserProfiles",
                column: "ConsultantLowRatingFlaggedAt",
                filter: "[ConsultantLowRatingFlaggedAt] IS NOT NULL");

            // Backfill the snapshot from existing visible reviews so already-rated
            // consultants show their average immediately (factor defaults to 1.0, so
            // penalized == raw for pre-existing rows). New rows are kept current by
            // ConsultantRatingService on every rating submit / penalty event.
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.ConsultantAverageRating =
                        ROUND(CAST(agg.AvgRating AS decimal(3,2)), 2),
                    p.ConsultantReviewCount = agg.Cnt
                FROM UserProfiles p
                INNER JOIN (
                    SELECT ConsultantId,
                           AVG(CAST(Rating AS decimal(3,2))) AS AvgRating,
                           COUNT(*) AS Cnt
                    FROM ConsultantReviews
                    WHERE IsHiddenByAdmin = 0 AND IsDeleted = 0
                    GROUP BY ConsultantId
                ) agg ON agg.ConsultantId = p.UserId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_ConsultantLowRatingFlagged",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ConsultantAverageRating",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ConsultantLowRatingFlaggedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ConsultantRatingPenaltyFactor",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ConsultantReviewCount",
                table: "UserProfiles");
        }
    }
}
