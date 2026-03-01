using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsOnboardingComplete = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    MaxMembers = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MessageAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SuccessStories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuccessStories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuccessStories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UpgradeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExperienceSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpertiseTags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Languages = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PortfolioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CompanyCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyWebsite = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactPersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProofDocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldOfStudy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GPA = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    Interests = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Scholarships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FieldOfStudy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FundingType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DegreeLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AwardAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EligibilityDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequiredDocuments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OfficialLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MinGPA = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    MaxAge = table.Column<int>(type: "int", nullable: true),
                    EligibleCountries = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EligibleMajors = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scholarships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scholarships_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.CheckConstraint("CK_Messages_ReceiverOrGroup", "(ReceiverId IS NOT NULL AND GroupId IS NULL) OR (ReceiverId IS NULL AND GroupId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Messages_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Posts_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SavedScholarships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScholarshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedScholarships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedScholarships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SavedScholarships_Scholarships_ScholarshipId",
                        column: x => x.ScholarshipId,
                        principalTable: "Scholarships",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Comments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Likes", x => x.Id);
                    table.CheckConstraint("CK_Likes_PostOrComment", "(PostId IS NOT NULL AND CommentId IS NULL) OR (PostId IS NULL AND CommentId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Likes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Likes_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Likes_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "AccountStatus", "ConcurrencyStamp", "CreatedAt", "Email", "EmailConfirmed", "FirstName", "IsActive", "IsOnboardingComplete", "LastLoginAt", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "ProfileImageUrl", "Role", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 0, "Active", "ADMIN-CONCURRENCY-STAMP-00001", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@scholarpath.com", true, "System", true, true, null, "Admin", false, null, "ADMIN@SCHOLARPATH.COM", "ADMIN@SCHOLARPATH.COM", "AQAAAAIAAYagAAAAEBzopFBef9EbyXq08be+PDy9bpasNFbYqmiSYXxvLVk3ydlcbIBNF4KeMFkqu9/OTQ==", null, false, null, "Admin", "ADMIN-SECURITY-STAMP-00000001", false, "admin@scholarpath.com" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 0, "Active", "STUDENT-CONCURRENCY-STAMP-002", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ahmed.student@scholarpath.com", true, "Ahmed", true, true, null, "Hassan", false, null, "AHMED.STUDENT@SCHOLARPATH.COM", "AHMED.STUDENT@SCHOLARPATH.COM", "AQAAAAIAAYagAAAAENnfSCXEfIvwmiulLqrD69j7vHG3dq+P/5wbiM/k9bbUZWruE2CSXXWvcSqYkj7Niw==", null, false, null, "Student", "STUDENT-SECURITY-STAMP-0000002", false, "ahmed.student@scholarpath.com" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 0, "Active", "CONSULT-CONCURRENCY-STAMP-03", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "sara.consultant@scholarpath.com", true, "Sara", true, true, null, "Mohamed", false, null, "SARA.CONSULTANT@SCHOLARPATH.COM", "SARA.CONSULTANT@SCHOLARPATH.COM", "AQAAAAIAAYagAAAAEITdr5Aifj2zBzl4OM+yP6AWgGMc3zPUSFi6HUXZjzY4WXk9/CvMASxZIx8r5cEekg==", null, false, null, "Consultant", "CONSULT-SECURITY-STAMP-000003", false, "sara.consultant@scholarpath.com" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), 0, "Active", "COMPANY-CONCURRENCY-STAMP-004", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "omar.company@scholarpath.com", true, "Omar", true, true, null, "Khalil", false, null, "OMAR.COMPANY@SCHOLARPATH.COM", "OMAR.COMPANY@SCHOLARPATH.COM", "AQAAAAIAAYagAAAAEPrDShx08aOmDP6z/OLyYXfyF7pvJM1EDbsV9wicesVdlZqTNVN3k/XtUobxDoLQqg==", null, false, null, "Company", "COMPANY-SECURITY-STAMP-000004", false, "omar.company@scholarpath.com" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "DeletedBy", "Description", "DescriptionAr", "IsDeleted", "Name", "NameAr", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Science, Technology, Engineering, and Mathematics scholarships", "منح العلوم والتكنولوجيا والهندسة والرياضيات", false, "STEM", "العلوم والتكنولوجيا", null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Scholarships for arts, literature, history, and philosophy", "منح الفنون والأدب والتاريخ والفلسفة", false, "Arts & Humanities", "الفنون والعلوم الإنسانية", null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Business administration, finance, and economics scholarships", "منح إدارة الأعمال والمالية والاقتصاد", false, "Business & Economics", "الأعمال والاقتصاد", null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Medical, nursing, pharmacy, and public health scholarships", "منح الطب والتمريض والصيدلة والصحة العامة", false, "Medicine & Health Sciences", "الطب والعلوم الصحية", null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Civil, mechanical, electrical, and software engineering scholarships", "منح الهندسة المدنية والميكانيكية والكهربائية وهندسة البرمجيات", false, "Engineering", "الهندسة", null },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Legal studies and international law scholarships", "منح الدراسات القانونية والقانون الدولي", false, "Law", "القانون", null },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Teaching, pedagogy, and educational leadership scholarships", "منح التدريس والتربية والقيادة التعليمية", false, "Education", "التعليم", null },
                    { new Guid("20000000-0000-0000-0000-000000000008"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Psychology, sociology, political science, and anthropology scholarships", "منح علم النفس والاجتماع والعلوم السياسية والأنثروبولوجيا", false, "Social Sciences", "العلوم الاجتماعية", null }
                });

            migrationBuilder.InsertData(
                table: "Resources",
                columns: new[] { "Id", "Category", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "DescriptionAr", "IsDeleted", "Title", "TitleAr", "Type", "UpdatedAt", "UpdatedBy", "Url" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), "Application Tips", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "A comprehensive guide covering structure, tone, and common mistakes to avoid when writing scholarship personal statements.", "دليل شامل يغطي الهيكل والأسلوب والأخطاء الشائعة التي يجب تجنبها عند كتابة بيانات المنح الشخصية.", false, "How to Write a Winning Scholarship Essay", "كيف تكتب مقالة منحة دراسية ناجحة", "Article", null, null, "https://scholarpath.com/resources/scholarship-essay-guide" },
                    { new Guid("50000000-0000-0000-0000-000000000002"), "Test Preparation", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "A 30-day structured study plan for IELTS preparation with practice materials and tips for each section.", "خطة دراسية منظمة لمدة 30 يومًا للتحضير لامتحان IELTS مع مواد تدريبية ونصائح لكل قسم.", false, "IELTS Preparation: Free Study Plan", "التحضير لامتحان IELTS: خطة دراسية مجانية", "Guide", null, null, "https://scholarpath.com/resources/ielts-study-plan" },
                    { new Guid("50000000-0000-0000-0000-000000000003"), "Scholarship Search", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "A curated list of the best online scholarship search engines and databases specifically useful for students from the Arab region.", "قائمة منسقة لأفضل محركات البحث وقواعد البيانات للمنح الدراسية المفيدة تحديدًا للطلاب من المنطقة العربية.", false, "Top 10 Scholarship Databases for Arab Students", "أفضل 10 قواعد بيانات للمنح الدراسية للطلاب العرب", "Article", null, null, "https://scholarpath.com/resources/scholarship-databases" },
                    { new Guid("50000000-0000-0000-0000-000000000004"), "Application Tips", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Professional email templates and tips for requesting strong recommendation letters from professors and employers.", "قوالب بريد إلكتروني احترافية ونصائح لطلب خطابات توصية قوية من الأساتذة وأصحاب العمل.", false, "Recommendation Letter Request Template", "نموذج طلب خطاب التوصية", "Template", null, null, "https://scholarpath.com/resources/recommendation-letter-template" }
                });

            migrationBuilder.InsertData(
                table: "Groups",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "CreatorId", "DeletedAt", "DeletedBy", "Description", "DescriptionAr", "ImageUrl", "IsDeleted", "IsPrivate", "MaxMembers", "Name", "NameAr", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("60000000-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("10000000-0000-0000-0000-000000000003"), null, null, "A community for students applying to DAAD scholarships. Share tips, experiences, and support each other through the application process.", "مجتمع للطلاب المتقدمين لمنح DAAD. شاركوا النصائح والخبرات وادعموا بعضكم البعض خلال عملية التقديم.", null, false, false, 200, "DAAD Scholarship Applicants", "متقدمو منحة DAAD", null, null },
                    { new Guid("60000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("10000000-0000-0000-0000-000000000001"), null, null, "Everything about studying in the United Kingdom: university selection, visa process, living costs, and scholarship opportunities.", "كل ما يتعلق بالدراسة في المملكة المتحدة: اختيار الجامعة وإجراءات التأشيرة وتكاليف المعيشة وفرص المنح الدراسية.", null, false, false, 500, "Study in the UK", "الدراسة في بريطانيا", null, null }
                });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "CreatedAt", "IsRead", "Message", "MessageAr", "ReadAt", "RelatedEntityId", "RelatedEntityType", "Title", "TitleAr", "Type", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("70000000-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Your account has been activated. Start exploring scholarships and join communities to connect with fellow students.", "تم تفعيل حسابك. ابدأ في استكشاف المنح الدراسية وانضم إلى المجتمعات للتواصل مع زملائك الطلاب.", null, null, null, "Welcome to ScholarPath!", "مرحبًا بك في ScholarPath!", "System", null, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("70000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "A new fully-funded scholarship matching your profile has been posted. Check it out before the deadline!", "تم نشر منحة ممولة بالكامل جديدة تتوافق مع ملفك الشخصي. تحقق منها قبل الموعد النهائي!", null, new Guid("30000000-0000-0000-0000-000000000001"), "Scholarship", "New Scholarship: DAAD Graduate Scholarship", "منحة جديدة: منحة DAAD للدراسات العليا", "ScholarshipAlert", null, new Guid("10000000-0000-0000-0000-000000000002") }
                });

            migrationBuilder.InsertData(
                table: "Scholarships",
                columns: new[] { "Id", "AwardAmount", "CategoryId", "Country", "CreatedAt", "CreatedBy", "Currency", "Deadline", "DegreeLevel", "DeletedAt", "DeletedBy", "Description", "DescriptionAr", "EligibilityDescription", "EligibleCountries", "EligibleMajors", "FieldOfStudy", "FundingType", "ImageUrl", "IsActive", "IsDeleted", "MaxAge", "MinGPA", "OfficialLink", "RequiredDocuments", "Title", "TitleAr", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), 1200m, new Guid("20000000-0000-0000-0000-000000000001"), "Germany", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "EUR", new DateTime(2026, 10, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Masters", null, null, "The German Academic Exchange Service (DAAD) offers fully funded scholarships for international students pursuing Master's or PhD degrees in Germany. The scholarship covers tuition, monthly stipend, health insurance, and travel costs.", "تقدم هيئة التبادل الأكاديمي الألمانية (DAAD) منحًا ممولة بالكامل للطلاب الدوليين لمتابعة درجة الماجستير أو الدكتوراه في ألمانيا. تغطي المنحة الرسوم الدراسية والراتب الشهري والتأمين الصحي وتكاليف السفر.", "Open to graduates from developing countries with excellent academic records. Applicants must hold a Bachelor's degree and have at least 2 years of work experience.", "[\"Egypt\",\"Jordan\",\"Morocco\",\"Tunisia\",\"Lebanon\"]", "[\"Computer Science\",\"Engineering\",\"Natural Sciences\",\"Mathematics\"]", "All Fields", "FullyFunded", null, true, false, null, 3.0m, "https://www.daad.de/en/study-and-research-in-germany/scholarships/", "CV, motivation letter, academic transcripts, recommendation letters, language certificate", "DAAD Graduate Scholarship", "منحة DAAD للدراسات العليا", null, null },
                    { new Guid("30000000-0000-0000-0000-000000000002"), 18000m, new Guid("20000000-0000-0000-0000-000000000003"), "United Kingdom", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "GBP", new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Masters", null, null, "Chevening is the UK government's international awards programme offering fully funded Master's degrees at any UK university. It provides a unique opportunity for future leaders and decision-makers.", "تشيفنينج هو برنامج المنح الدولي للحكومة البريطانية الذي يقدم درجات ماجستير ممولة بالكامل في أي جامعة بريطانية. يوفر فرصة فريدة للقادة وصناع القرار المستقبليين.", "Applicants must have at least 2 years of work experience, hold an undergraduate degree, and return to their home country for at least 2 years after the scholarship.", "[\"Egypt\",\"Jordan\",\"Iraq\",\"Saudi Arabia\",\"UAE\"]", null, "All Fields", "FullyFunded", null, true, false, null, 3.0m, "https://www.chevening.org/", "Personal statement, reference letters, academic transcripts, English language test", "Chevening Scholarship", "منحة تشيفنينج", null, null },
                    { new Guid("30000000-0000-0000-0000-000000000003"), 800m, new Guid("20000000-0000-0000-0000-000000000007"), "Turkey", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "TRY", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Bachelors", null, null, "The Turkish Government offers fully funded scholarships for international students at all academic levels. The program includes Turkish language preparation, tuition waiver, monthly stipend, accommodation, and health insurance.", "تقدم الحكومة التركية منحًا ممولة بالكامل للطلاب الدوليين في جميع المراحل الأكاديمية. يشمل البرنامج إعداد اللغة التركية والإعفاء من الرسوم والراتب الشهري والسكن والتأمين الصحي.", "Open to citizens of all countries except Turkey. Undergraduate applicants must be under 21, Master's under 30, and PhD under 35.", "[\"Egypt\",\"Palestine\",\"Syria\",\"Yemen\",\"Somalia\"]", null, "All Fields", "FullyFunded", null, true, false, 21, null, "https://www.turkiyeburslari.gov.tr/", "National ID, photo, high school diploma, transcripts, language certificate (optional)", "Turkish Government Scholarship (Turkiye Burslari)", "المنحة التركية الحكومية", null, null },
                    { new Guid("30000000-0000-0000-0000-000000000004"), 35000m, new Guid("20000000-0000-0000-0000-000000000008"), "United States", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "USD", new DateTime(2026, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Masters", null, null, "The Fulbright Program provides grants for graduate students to study, conduct research, or teach English in the United States. It is one of the most prestigious scholarship programs in the world.", "يقدم برنامج فولبرايت منحًا لطلاب الدراسات العليا للدراسة أو إجراء البحوث أو تدريس اللغة الإنجليزية في الولايات المتحدة. وهو واحد من أكثر برامج المنح الدراسية المرموقة في العالم.", "Candidates must hold a Bachelor's degree, demonstrate English proficiency, and have a strong academic and professional record.", "[\"Egypt\",\"Lebanon\",\"Jordan\",\"Morocco\",\"Tunisia\",\"Iraq\"]", "[\"Public Policy\",\"Engineering\",\"Sciences\",\"Education\",\"Arts\"]", "All Fields", "FullyFunded", null, true, false, null, 3.0m, "https://foreign.fulbrightonline.org/", "Application form, personal statement, study plan, recommendation letters, TOEFL/IELTS score", "Fulbright Foreign Student Program", "برنامج فولبرايت للطلاب الأجانب", null, null },
                    { new Guid("30000000-0000-0000-0000-000000000005"), 25000m, new Guid("20000000-0000-0000-0000-000000000005"), "European Union", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "EUR", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Masters", null, null, "Erasmus Mundus Joint Master Degrees are prestigious integrated study programmes delivered by international consortia of higher education institutions. Students study in at least two different European countries.", "درجات ماجستير إيراسموس موندوس المشتركة هي برامج دراسية متكاملة مرموقة تقدمها اتحادات دولية لمؤسسات التعليم العالي. يدرس الطلاب في دولتين أوروبيتين على الأقل.", "Open to students and scholars worldwide. Applicants must hold a recognized Bachelor's degree. Partner country applicants receive higher funding.", "[\"Egypt\",\"Algeria\",\"Libya\",\"Sudan\",\"Mauritania\"]", "[\"Environmental Science\",\"Public Health\",\"Data Science\",\"Urban Planning\"]", "Multiple Fields", "FullyFunded", null, true, false, null, 3.2m, "https://erasmus-plus.ec.europa.eu/", "Online application, academic transcripts, CV, motivation letter, language proficiency proof, recommendation letters", "Erasmus Mundus Joint Masters", "منح إيراسموس موندوس للماجستير المشترك", null, null }
                });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "Bio", "Country", "CreatedAt", "CreatedBy", "DateOfBirth", "FieldOfStudy", "GPA", "Interests", "TargetCountry", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), "Passionate computer science student seeking international scholarship opportunities.", "Egypt", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Computer Science", 3.75m, "[\"AI\",\"Machine Learning\",\"Web Development\"]", "Germany", null, null, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("40000000-0000-0000-0000-000000000002"), "Experienced scholarship consultant with 10+ years helping students secure funding.", "Egypt", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "International Education", null, null, null, null, null, new Guid("10000000-0000-0000-0000-000000000003") }
                });

            migrationBuilder.InsertData(
                table: "GroupMembers",
                columns: new[] { "Id", "CreatedAt", "GroupId", "Role", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("61000000-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("60000000-0000-0000-0000-000000000001"), "Admin", null, new Guid("10000000-0000-0000-0000-000000000003") },
                    { new Guid("61000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("60000000-0000-0000-0000-000000000001"), "Member", null, new Guid("10000000-0000-0000-0000-000000000002") },
                    { new Guid("61000000-0000-0000-0000-000000000003"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("60000000-0000-0000-0000-000000000002"), "Admin", null, new Guid("10000000-0000-0000-0000-000000000001") },
                    { new Guid("61000000-0000-0000-0000-000000000004"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("60000000-0000-0000-0000-000000000002"), "Member", null, new Guid("10000000-0000-0000-0000-000000000002") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Role",
                table: "AspNetUsers",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorId",
                table: "Comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId",
                table: "Comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId",
                table: "GroupMembers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId_GroupId",
                table: "GroupMembers",
                columns: new[] { "UserId", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CreatorId",
                table: "Groups",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name",
                table: "Groups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_CommentId",
                table: "Likes",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_PostId",
                table: "Likes",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId_CommentId",
                table: "Likes",
                columns: new[] { "UserId", "CommentId" },
                unique: true,
                filter: "CommentId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId_PostId",
                table: "Likes",
                columns: new[] { "UserId", "PostId" },
                unique: true,
                filter: "PostId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedAt",
                table: "Messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_GroupId",
                table: "Messages",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_GroupId",
                table: "Posts",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_GroupId_CreatedAt",
                table: "Posts",
                columns: new[] { "GroupId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedScholarships_ScholarshipId",
                table: "SavedScholarships",
                column: "ScholarshipId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedScholarships_UserId_ScholarshipId",
                table: "SavedScholarships",
                columns: new[] { "UserId", "ScholarshipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_CategoryId",
                table: "Scholarships",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_Country",
                table: "Scholarships",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_Deadline",
                table: "Scholarships",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_DegreeLevel",
                table: "Scholarships",
                column: "DegreeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_FieldOfStudy",
                table: "Scholarships",
                column: "FieldOfStudy");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_FundingType",
                table: "Scholarships",
                column: "FundingType");

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_IsActive",
                table: "Scholarships",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessStories_IsApproved",
                table: "SuccessStories",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessStories_UserId",
                table: "SuccessStories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequests_Status",
                table: "UpgradeRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequests_UserId",
                table: "UpgradeRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "SavedScholarships");

            migrationBuilder.DropTable(
                name: "SuccessStories");

            migrationBuilder.DropTable(
                name: "UpgradeRequests");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Scholarships");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
