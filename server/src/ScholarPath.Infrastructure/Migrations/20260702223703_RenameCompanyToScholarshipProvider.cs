using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <summary>
    /// Renames the "Company" scholarship-provider vocabulary to "ScholarshipProvider"
    /// across the physical schema — WITHOUT dropping/recreating any table, so all
    /// existing rows are preserved. EF's default scaffold emitted DropTable+CreateTable
    /// for the three renamed tables (which would lose data); this migration replaces
    /// that with in-place RenameTable / RenameColumn / RenameIndex plus sp_rename for
    /// the primary-key and foreign-key constraint names, and finally rewrites the
    /// persisted string values (role name, enum values, provider org-type).
    ///
    /// ⚠ Physical-schema rename: coordinate with the CDC + analytics redeploy
    /// (see docs/runbooks) — the CDC capture instances and dbt/ADF/Power BI sources
    /// reference the OLD table/column names and must be updated in the same window.
    /// </summary>
    public partial class RenameCompanyToScholarshipProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── UserProfiles + Scholarships column renames (data-preserving) ──────────
            // The filtered index depends on the renamed column, so drop → rename → recreate.
            migrationBuilder.DropForeignKey(
                name: "FK_Scholarships_Users_OwnerCompanyId",
                table: "Scholarships");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_CompanyLowRatingFlagged",
                table: "UserProfiles");

            migrationBuilder.RenameColumn(name: "CompanyType", table: "UserProfiles", newName: "ScholarshipProviderType");
            migrationBuilder.RenameColumn(name: "CompanyReviewCount", table: "UserProfiles", newName: "ScholarshipProviderReviewCount");
            migrationBuilder.RenameColumn(name: "CompanyLowRatingFlaggedAt", table: "UserProfiles", newName: "ScholarshipProviderLowRatingFlaggedAt");
            migrationBuilder.RenameColumn(name: "CompanyDescription", table: "UserProfiles", newName: "ScholarshipProviderDescription");
            migrationBuilder.RenameColumn(name: "CompanyAverageRating", table: "UserProfiles", newName: "ScholarshipProviderAverageRating");

            migrationBuilder.RenameColumn(name: "OwnerCompanyId", table: "Scholarships", newName: "OwnerScholarshipProviderId");
            migrationBuilder.RenameIndex(name: "IX_Scholarships_OwnerCompanyId", table: "Scholarships", newName: "IX_Scholarships_OwnerScholarshipProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ScholarshipProviderLowRatingFlagged",
                table: "UserProfiles",
                column: "ScholarshipProviderLowRatingFlaggedAt",
                filter: "[ScholarshipProviderLowRatingFlaggedAt] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Scholarships_Users_OwnerScholarshipProviderId",
                table: "Scholarships",
                column: "OwnerScholarshipProviderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ── Table 1: CompanyReviewPayments → ScholarshipProviderReviewPayments ─────
            migrationBuilder.RenameTable(name: "CompanyReviewPayments", newName: "ScholarshipProviderReviewPayments");
            migrationBuilder.RenameColumn(name: "CompanyId", table: "ScholarshipProviderReviewPayments", newName: "ScholarshipProviderId");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviewPayments_IdempotencyKey", table: "ScholarshipProviderReviewPayments", newName: "IX_ScholarshipProviderReviewPayments_IdempotencyKey");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviewPayments_StripePaymentIntentId", table: "ScholarshipProviderReviewPayments", newName: "IX_ScholarshipProviderReviewPayments_StripePaymentIntentId");
            migrationBuilder.Sql("EXEC sp_rename N'PK_CompanyReviewPayments', N'PK_ScholarshipProviderReviewPayments';");

            // ── Table 2: CompanyReviewRequests → ScholarshipProviderReviewRequests ─────
            migrationBuilder.RenameTable(name: "CompanyReviewRequests", newName: "ScholarshipProviderReviewRequests");
            migrationBuilder.RenameColumn(name: "CompanyId", table: "ScholarshipProviderReviewRequests", newName: "ScholarshipProviderId");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviewRequests_CompanyId_Status", table: "ScholarshipProviderReviewRequests", newName: "IX_ScholarshipProviderReviewRequests_ScholarshipProviderId_Status");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviewRequests_PaymentId", table: "ScholarshipProviderReviewRequests", newName: "IX_ScholarshipProviderReviewRequests_PaymentId");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviewRequests_ScholarshipId", table: "ScholarshipProviderReviewRequests", newName: "IX_ScholarshipProviderReviewRequests_ScholarshipId");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviewRequests_StudentId_Status", table: "ScholarshipProviderReviewRequests", newName: "IX_ScholarshipProviderReviewRequests_StudentId_Status");
            migrationBuilder.RenameIndex(name: "UX_CompanyReviewRequests_Student_Scholarship_Active", table: "ScholarshipProviderReviewRequests", newName: "UX_ScholarshipProviderReviewRequests_Student_Scholarship_Active");
            migrationBuilder.Sql("EXEC sp_rename N'PK_CompanyReviewRequests', N'PK_ScholarshipProviderReviewRequests';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_CompanyReviewRequests_Payments_PaymentId', N'FK_ScholarshipProviderReviewRequests_Payments_PaymentId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_CompanyReviewRequests_Scholarships_ScholarshipId', N'FK_ScholarshipProviderReviewRequests_Scholarships_ScholarshipId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_CompanyReviewRequests_Users_CompanyId', N'FK_ScholarshipProviderReviewRequests_Users_ScholarshipProviderId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_CompanyReviewRequests_Users_StudentId', N'FK_ScholarshipProviderReviewRequests_Users_StudentId';");

            // ── Table 3: CompanyReviews → ScholarshipProviderReviews ──────────────────
            migrationBuilder.RenameTable(name: "CompanyReviews", newName: "ScholarshipProviderReviews");
            migrationBuilder.RenameColumn(name: "CompanyId", table: "ScholarshipProviderReviews", newName: "ScholarshipProviderId");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviews_ApplicationTrackerId", table: "ScholarshipProviderReviews", newName: "IX_ScholarshipProviderReviews_ApplicationTrackerId");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviews_CompanyId_IsHiddenByAdmin_IsDeleted", table: "ScholarshipProviderReviews", newName: "IX_ScholarshipProviderReviews_ScholarshipProviderId_IsHiddenByAdmin_IsDeleted");
            migrationBuilder.RenameIndex(name: "IX_CompanyReviews_StudentId", table: "ScholarshipProviderReviews", newName: "IX_ScholarshipProviderReviews_StudentId");
            migrationBuilder.Sql("EXEC sp_rename N'PK_CompanyReviews', N'PK_ScholarshipProviderReviews';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_CompanyReviews_Applications_ApplicationTrackerId', N'FK_ScholarshipProviderReviews_Applications_ApplicationTrackerId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_CompanyReviews_Users_CompanyId', N'FK_ScholarshipProviderReviews_Users_ScholarshipProviderId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_CompanyReviews_Users_StudentId', N'FK_ScholarshipProviderReviews_Users_StudentId';");

            // Widen columns that must hold the longer "ScholarshipProvider*" enum
            // strings BEFORE rewriting their values. UpgradeRequests.Target is
            // nvarchar(16) but "ScholarshipProvider" is 19 chars, which would
            // otherwise truncate and fail the migration.
            migrationBuilder.AlterColumn<string>(
                name: "Target", table: "UpgradeRequests",
                type: "nvarchar(32)", maxLength: 32, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(16)", oldMaxLength: 16);

            // ── Persisted string values (role name, enum values, provider org-type) ───
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'ScholarshipProvider', [NormalizedName] = 'SCHOLARSHIPPROVIDER' WHERE [Name] = 'Company';");
            migrationBuilder.Sql("UPDATE [Users] SET [ActiveRole] = 'ScholarshipProvider' WHERE [ActiveRole] = 'Company';");
            migrationBuilder.Sql("UPDATE [Resources] SET [AuthorRole] = 'ScholarshipProvider' WHERE [AuthorRole] = 'Company';");
            migrationBuilder.Sql("UPDATE [Payments] SET [Type] = 'ScholarshipProviderReview' WHERE [Type] = 'CompanyReview';");
            migrationBuilder.Sql("UPDATE [UpgradeRequests] SET [Target] = 'ScholarshipProvider' WHERE [Target] = 'Company';");
            migrationBuilder.Sql("UPDATE [ScholarshipProviderReviewRequests] SET [Status] = 'RejectedByScholarshipProvider' WHERE [Status] = 'RejectedByCompany';");
            // The provider organisation-type option "Company" (a corporate provider) becomes "Corporation".
            migrationBuilder.Sql("UPDATE [UserProfiles] SET [ScholarshipProviderType] = 'Corporation' WHERE [ScholarshipProviderType] = 'Company';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse persisted string values.
            migrationBuilder.Sql("UPDATE [UserProfiles] SET [ScholarshipProviderType] = 'Company' WHERE [ScholarshipProviderType] = 'Corporation';");
            migrationBuilder.Sql("UPDATE [ScholarshipProviderReviewRequests] SET [Status] = 'RejectedByCompany' WHERE [Status] = 'RejectedByScholarshipProvider';");
            migrationBuilder.Sql("UPDATE [UpgradeRequests] SET [Target] = 'Company' WHERE [Target] = 'ScholarshipProvider';");
            migrationBuilder.AlterColumn<string>(
                name: "Target", table: "UpgradeRequests",
                type: "nvarchar(16)", maxLength: 16, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(32)", oldMaxLength: 32);
            migrationBuilder.Sql("UPDATE [Payments] SET [Type] = 'CompanyReview' WHERE [Type] = 'ScholarshipProviderReview';");
            migrationBuilder.Sql("UPDATE [Resources] SET [AuthorRole] = 'Company' WHERE [AuthorRole] = 'ScholarshipProvider';");
            migrationBuilder.Sql("UPDATE [Users] SET [ActiveRole] = 'Company' WHERE [ActiveRole] = 'ScholarshipProvider';");
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'Company', [NormalizedName] = 'COMPANY' WHERE [Name] = 'ScholarshipProvider';");

            // Table 3 back.
            migrationBuilder.Sql("EXEC sp_rename N'FK_ScholarshipProviderReviews_Users_StudentId', N'FK_CompanyReviews_Users_StudentId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_ScholarshipProviderReviews_Users_ScholarshipProviderId', N'FK_CompanyReviews_Users_CompanyId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_ScholarshipProviderReviews_Applications_ApplicationTrackerId', N'FK_CompanyReviews_Applications_ApplicationTrackerId';");
            migrationBuilder.Sql("EXEC sp_rename N'PK_ScholarshipProviderReviews', N'PK_CompanyReviews';");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviews_StudentId", table: "ScholarshipProviderReviews", newName: "IX_CompanyReviews_StudentId");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviews_ScholarshipProviderId_IsHiddenByAdmin_IsDeleted", table: "ScholarshipProviderReviews", newName: "IX_CompanyReviews_CompanyId_IsHiddenByAdmin_IsDeleted");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviews_ApplicationTrackerId", table: "ScholarshipProviderReviews", newName: "IX_CompanyReviews_ApplicationTrackerId");
            migrationBuilder.RenameColumn(name: "ScholarshipProviderId", table: "ScholarshipProviderReviews", newName: "CompanyId");
            migrationBuilder.RenameTable(name: "ScholarshipProviderReviews", newName: "CompanyReviews");

            // Table 2 back.
            migrationBuilder.Sql("EXEC sp_rename N'FK_ScholarshipProviderReviewRequests_Users_StudentId', N'FK_CompanyReviewRequests_Users_StudentId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_ScholarshipProviderReviewRequests_Users_ScholarshipProviderId', N'FK_CompanyReviewRequests_Users_CompanyId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_ScholarshipProviderReviewRequests_Scholarships_ScholarshipId', N'FK_CompanyReviewRequests_Scholarships_ScholarshipId';");
            migrationBuilder.Sql("EXEC sp_rename N'FK_ScholarshipProviderReviewRequests_Payments_PaymentId', N'FK_CompanyReviewRequests_Payments_PaymentId';");
            migrationBuilder.Sql("EXEC sp_rename N'PK_ScholarshipProviderReviewRequests', N'PK_CompanyReviewRequests';");
            migrationBuilder.RenameIndex(name: "UX_ScholarshipProviderReviewRequests_Student_Scholarship_Active", table: "ScholarshipProviderReviewRequests", newName: "UX_CompanyReviewRequests_Student_Scholarship_Active");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviewRequests_StudentId_Status", table: "ScholarshipProviderReviewRequests", newName: "IX_CompanyReviewRequests_StudentId_Status");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviewRequests_ScholarshipId", table: "ScholarshipProviderReviewRequests", newName: "IX_CompanyReviewRequests_ScholarshipId");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviewRequests_PaymentId", table: "ScholarshipProviderReviewRequests", newName: "IX_CompanyReviewRequests_PaymentId");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviewRequests_ScholarshipProviderId_Status", table: "ScholarshipProviderReviewRequests", newName: "IX_CompanyReviewRequests_CompanyId_Status");
            migrationBuilder.RenameColumn(name: "ScholarshipProviderId", table: "ScholarshipProviderReviewRequests", newName: "CompanyId");
            migrationBuilder.RenameTable(name: "ScholarshipProviderReviewRequests", newName: "CompanyReviewRequests");

            // Table 1 back.
            migrationBuilder.Sql("EXEC sp_rename N'PK_ScholarshipProviderReviewPayments', N'PK_CompanyReviewPayments';");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviewPayments_StripePaymentIntentId", table: "ScholarshipProviderReviewPayments", newName: "IX_CompanyReviewPayments_StripePaymentIntentId");
            migrationBuilder.RenameIndex(name: "IX_ScholarshipProviderReviewPayments_IdempotencyKey", table: "ScholarshipProviderReviewPayments", newName: "IX_CompanyReviewPayments_IdempotencyKey");
            migrationBuilder.RenameColumn(name: "ScholarshipProviderId", table: "ScholarshipProviderReviewPayments", newName: "CompanyId");
            migrationBuilder.RenameTable(name: "ScholarshipProviderReviewPayments", newName: "CompanyReviewPayments");

            // UserProfiles + Scholarships back.
            migrationBuilder.DropForeignKey(name: "FK_Scholarships_Users_OwnerScholarshipProviderId", table: "Scholarships");
            migrationBuilder.DropIndex(name: "IX_UserProfiles_ScholarshipProviderLowRatingFlagged", table: "UserProfiles");
            migrationBuilder.RenameColumn(name: "ScholarshipProviderType", table: "UserProfiles", newName: "CompanyType");
            migrationBuilder.RenameColumn(name: "ScholarshipProviderReviewCount", table: "UserProfiles", newName: "CompanyReviewCount");
            migrationBuilder.RenameColumn(name: "ScholarshipProviderLowRatingFlaggedAt", table: "UserProfiles", newName: "CompanyLowRatingFlaggedAt");
            migrationBuilder.RenameColumn(name: "ScholarshipProviderDescription", table: "UserProfiles", newName: "CompanyDescription");
            migrationBuilder.RenameColumn(name: "ScholarshipProviderAverageRating", table: "UserProfiles", newName: "CompanyAverageRating");
            migrationBuilder.RenameColumn(name: "OwnerScholarshipProviderId", table: "Scholarships", newName: "OwnerCompanyId");
            migrationBuilder.RenameIndex(name: "IX_Scholarships_OwnerScholarshipProviderId", table: "Scholarships", newName: "IX_Scholarships_OwnerCompanyId");
            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CompanyLowRatingFlagged",
                table: "UserProfiles",
                column: "CompanyLowRatingFlaggedAt",
                filter: "[CompanyLowRatingFlaggedAt] IS NOT NULL");
            migrationBuilder.AddForeignKey(
                name: "FK_Scholarships_Users_OwnerCompanyId",
                table: "Scholarships",
                column: "OwnerCompanyId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
