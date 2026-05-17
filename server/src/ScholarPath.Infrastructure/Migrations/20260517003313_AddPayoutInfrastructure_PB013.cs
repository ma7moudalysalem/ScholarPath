using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutInfrastructure_PB013 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "UserProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StripeConnectOnboardedAt",
                table: "UserProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectStatus",
                table: "UserProfiles",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<Guid>(
                name: "PayoutId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProfitShareConfig_ActivePerType",
                table: "ProfitShareConfigs",
                column: "PaymentType",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayoutId",
                table: "Payments",
                column: "PayoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ProfitShareConfig_ActivePerType",
                table: "ProfitShareConfigs");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PayoutId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "StripeConnectOnboardedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "StripeConnectStatus",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "PayoutId",
                table: "Payments");
        }
    }
}
