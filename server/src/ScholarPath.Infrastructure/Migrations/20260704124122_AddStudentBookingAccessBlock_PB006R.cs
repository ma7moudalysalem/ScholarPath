using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentBookingAccessBlock_PB006R : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingAccessStatus",
                table: "UserProfiles",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "BookingBlockReason",
                table: "UserProfiles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BookingBlockUntil",
                table: "UserProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_BookingBlockUntil",
                table: "UserProfiles",
                column: "BookingBlockUntil",
                filter: "[BookingBlockUntil] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_BookingBlockUntil",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BookingAccessStatus",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BookingBlockReason",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "BookingBlockUntil",
                table: "UserProfiles");
        }
    }
}
