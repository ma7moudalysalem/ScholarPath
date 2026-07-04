using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueNotificationIdempotencyKey_Audit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications");

            // Remove any duplicate (IdempotencyKey, Channel) rows that the old
            // check-then-act race may already have created, keeping the earliest —
            // otherwise the new UNIQUE index below cannot be built.
            migrationBuilder.Sql(@"
                WITH dups AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (
                               PARTITION BY [IdempotencyKey], [Channel]
                               ORDER BY [CreatedAt], [Id]) AS rn
                    FROM [Notifications]
                    WHERE [IdempotencyKey] IS NOT NULL
                )
                DELETE FROM [Notifications] WHERE [Id] IN (SELECT [Id] FROM dups WHERE rn > 1);");

            migrationBuilder.CreateIndex(
                name: "UX_Notifications_IdempotencyKey_Channel",
                table: "Notifications",
                columns: new[] { "IdempotencyKey", "Channel" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Notifications_IdempotencyKey_Channel",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications",
                column: "IdempotencyKey");
        }
    }
}
