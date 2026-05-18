using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBase_RAG : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Applications_Student_Scholarship_Active",
                table: "Applications");

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Embedding = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    EmbeddingDimensions = table.Column<int>(type: "int", nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IndexedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Applications_Student_Scholarship_Active",
                table: "Applications",
                columns: new[] { "StudentId", "ScholarshipId" },
                unique: true,
                filter: "[Status] <> 'Withdrawn' AND [Status] <> 'Rejected' AND [Status] <> 'Accepted'");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_SourceId",
                table: "KnowledgeDocuments",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_SourceType_SourceKey",
                table: "KnowledgeDocuments",
                columns: new[] { "SourceType", "SourceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");

            migrationBuilder.DropIndex(
                name: "UX_Applications_Student_Scholarship_Active",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "UX_Applications_Student_Scholarship_Active",
                table: "Applications",
                columns: new[] { "StudentId", "ScholarshipId" },
                unique: true,
                filter: "[Status] NOT IN ('6', '5', '4')");
        }
    }
}
