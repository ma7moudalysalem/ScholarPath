using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBookingMeetingUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingUrl",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingUrl",
                table: "Bookings",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }
    }
}
