using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchToScholarships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. إنشاء الفهرس الشامل (Full-Text Catalog)
            migrationBuilder.Sql(
                sql: "CREATE FULLTEXT CATALOG ScholarshipCatalog AS DEFAULT;",
                suppressTransaction: true);

            // 2. إنشاء الـ Full-Text Index على الأعمدة المطلوبة
            // تم اختيار اللغة العربية (1025) والإنجليزية (1033) لضمان دقة البحث
            migrationBuilder.Sql(
                sql: @"
                    CREATE FULLTEXT INDEX ON Scholarships (
                        TitleEn LANGUAGE 1033, 
                        TitleAr LANGUAGE 1025, 
                        DescriptionEn LANGUAGE 1033, 
                        DescriptionAr LANGUAGE 1025
                    ) 
                    KEY INDEX PK_Scholarships 
                    WITH STOPLIST = SYSTEM;",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // حذف الفهرس في حالة التراجع عن الـ Migration
            migrationBuilder.Sql(
                sql: "DROP FULLTEXT INDEX ON Scholarships;",
                suppressTransaction: true);

            // حذف الكتالوج
            migrationBuilder.Sql(
                sql: "DROP FULLTEXT CATALOG ScholarshipCatalog;",
                suppressTransaction: true);
        }
    }
}
