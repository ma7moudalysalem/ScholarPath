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
            // Full-Text Search is an optional SQL Server component. Create the
            // catalog + index only when it is installed; otherwise skip — the
            // scholarship search falls back to LIKE and the schema still builds
            // (e.g. local SQL Server without the FT feature).
            migrationBuilder.Sql(
                sql: """
                    IF (SELECT SERVERPROPERTY('IsFullTextInstalled')) = 1
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'ScholarshipCatalog')
                            EXEC('CREATE FULLTEXT CATALOG ScholarshipCatalog AS DEFAULT;');

                        IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Scholarships'))
                            EXEC('CREATE FULLTEXT INDEX ON Scholarships (
                                TitleEn LANGUAGE 1033,
                                TitleAr LANGUAGE 1025,
                                DescriptionEn LANGUAGE 1033,
                                DescriptionAr LANGUAGE 1025
                            ) KEY INDEX PK_Scholarships WITH STOPLIST = SYSTEM;');
                    END
                    """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                sql: """
                    IF (SELECT SERVERPROPERTY('IsFullTextInstalled')) = 1
                    BEGIN
                        IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Scholarships'))
                            EXEC('DROP FULLTEXT INDEX ON Scholarships;');

                        IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'ScholarshipCatalog')
                            EXEC('DROP FULLTEXT CATALOG ScholarshipCatalog;');
                    END
                    """,
                suppressTransaction: true);
        }
    }
}
