using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefineConsultantBookingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiredAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AvailabilityId",
                table: "Bookings",
                column: "AvailabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StripePaymentIntentId",
                table: "Bookings",
                column: "StripePaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_ConsultantId_DayOfWeek_StartTime_IsActive",
                table: "Availabilities",
                columns: new[] { "ConsultantId", "DayOfWeek", "StartTime", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_ConsultantId_SpecificStartAt_IsActive",
                table: "Availabilities",
                columns: new[] { "ConsultantId", "SpecificStartAt", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Availabilities_AvailabilityId",
                table: "Bookings",
                column: "AvailabilityId",
                principalTable: "Availabilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Availabilities_AvailabilityId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AvailabilityId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StripePaymentIntentId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Availabilities_ConsultantId_DayOfWeek_StartTime_IsActive",
                table: "Availabilities");

            migrationBuilder.DropIndex(
                name: "IX_Availabilities_ConsultantId_SpecificStartAt_IsActive",
                table: "Availabilities");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Bookings");
        }
    }
}
