using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingSlotUniqueIndex_PB006 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_ConsultantId_ScheduledStartAt",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "UX_Bookings_Consultant_Slot_Active",
                table: "Bookings",
                columns: new[] { "ConsultantId", "ScheduledStartAt" },
                unique: true,
                filter: "[Status] IN ('Requested', 'Confirmed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Bookings_Consultant_Slot_Active",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ConsultantId_ScheduledStartAt",
                table: "Bookings",
                columns: new[] { "ConsultantId", "ScheduledStartAt" });
        }
    }
}
