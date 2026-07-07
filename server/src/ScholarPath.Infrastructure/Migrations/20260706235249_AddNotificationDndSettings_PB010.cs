using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDndSettings_PB010 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationsMuted",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "QuietHoursEnabled",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "QuietHoursEnd",
                table: "UserProfiles",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "QuietHoursStart",
                table: "UserProfiles",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuietHoursTimezone",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationsMuted",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "QuietHoursEnabled",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "QuietHoursEnd",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "QuietHoursStart",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "QuietHoursTimezone",
                table: "UserProfiles");
        }
    }
}
