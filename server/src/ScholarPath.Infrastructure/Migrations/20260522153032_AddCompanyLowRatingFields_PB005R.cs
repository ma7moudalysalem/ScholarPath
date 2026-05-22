using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyLowRatingFields_PB005R : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CompanyAverageRating",
                table: "UserProfiles",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompanyLowRatingFlaggedAt",
                table: "UserProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyReviewCount",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CompanyLowRatingFlagged",
                table: "UserProfiles",
                column: "CompanyLowRatingFlaggedAt",
                filter: "[CompanyLowRatingFlaggedAt] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_CompanyLowRatingFlagged",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyAverageRating",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyLowRatingFlaggedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyReviewCount",
                table: "UserProfiles");
        }
    }
}
