using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingStudentNotes_PB006 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentNotes",
                table: "Bookings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudentNotes",
                table: "Bookings");
        }
    }
}
