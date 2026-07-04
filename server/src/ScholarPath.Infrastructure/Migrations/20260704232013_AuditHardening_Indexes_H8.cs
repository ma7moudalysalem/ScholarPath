using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditHardening_Indexes_H8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_NoShowReports_Booking_Accused",
                table: "NoShowReports");

            migrationBuilder.CreateIndex(
                name: "UX_NoShowReports_Booking_Accused",
                table: "NoShowReports",
                columns: new[] { "BookingId", "AccusedUserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AiInteractions_Feature_StartedAt",
                table: "AiInteractions",
                columns: new[] { "Feature", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_NoShowReports_Booking_Accused",
                table: "NoShowReports");

            migrationBuilder.DropIndex(
                name: "IX_AiInteractions_Feature_StartedAt",
                table: "AiInteractions");

            migrationBuilder.CreateIndex(
                name: "UX_NoShowReports_Booking_Accused",
                table: "NoShowReports",
                columns: new[] { "BookingId", "AccusedUserId" },
                unique: true);
        }
    }
}
