using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScholarshipProviderReviewRequestDocuments_PB005 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderFeedback",
                table: "ScholarshipProviderReviewRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScholarshipProviderReviewRequestId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ScholarshipProviderReviewRequestId",
                table: "Documents",
                column: "ScholarshipProviderReviewRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_ScholarshipProviderReviewRequests_ScholarshipProviderReviewRequestId",
                table: "Documents",
                column: "ScholarshipProviderReviewRequestId",
                principalTable: "ScholarshipProviderReviewRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_ScholarshipProviderReviewRequests_ScholarshipProviderReviewRequestId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ScholarshipProviderReviewRequestId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProviderFeedback",
                table: "ScholarshipProviderReviewRequests");

            migrationBuilder.DropColumn(
                name: "ScholarshipProviderReviewRequestId",
                table: "Documents");
        }
    }
}
