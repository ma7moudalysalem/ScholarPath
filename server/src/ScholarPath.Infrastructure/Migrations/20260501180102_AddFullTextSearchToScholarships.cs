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
            //   Full-Text Catalog
            migrationBuilder.Sql(
                sql: "CREATE FULLTEXT CATALOG ScholarshipCatalog AS DEFAULT;",
                suppressTransaction: true);

            // 2   Full-Text Index  
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
            // dleting the full-text index
            migrationBuilder.Sql(
                sql: "DROP FULLTEXT INDEX ON Scholarships;",
                suppressTransaction: true);

            // dleting the full-text cataiog
            migrationBuilder.Sql(
                sql: "DROP FULLTEXT CATALOG ScholarshipCatalog;",
                suppressTransaction: true);
        }
    }
}
