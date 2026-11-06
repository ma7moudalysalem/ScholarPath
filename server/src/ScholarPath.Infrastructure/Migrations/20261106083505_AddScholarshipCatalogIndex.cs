using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScholarshipCatalogIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_Catalog",
                table: "Scholarships",
                columns: new[] { "Status", "IsFeatured", "CreatedAt", "Id" },
                descending: new[] { false, true, true, false })
                .Annotation("SqlServer:Include", new[] { "Deadline" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scholarships_Catalog",
                table: "Scholarships");
        }
    }
}
