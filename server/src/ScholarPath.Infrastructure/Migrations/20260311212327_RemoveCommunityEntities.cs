using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScholarPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCommunityEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    MaxMembers = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    CommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                table: "Groups",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "CreatorId", "DeletedAt", "DeletedBy", "Description", "DescriptionAr", "ImageUrl", "IsDeleted", "IsPrivate", "MaxMembers", "Name", "NameAr", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("60000000-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("10000000-0000-0000-0000-000000000003"), null, null, "A community for students applying to DAAD scholarships. Share tips, experiences, and support each other through the application process.", "Ù…Ø¬ØªÙ…Ø¹ Ù„Ù„Ø·Ù„Ø§Ø¨ Ø§Ù„Ù…ØªÙ‚Ø¯Ù…ÙŠÙ† Ù„Ù…Ù†Ø­ DAAD. Ø´Ø§Ø±ÙƒÙˆØ§ Ø§Ù„Ù†ØµØ§Ø¦Ø­ ÙˆØ§Ù„Ø®Ø¨Ø±Ø§Øª ÙˆØ§Ø¯Ø¹Ù…ÙˆØ§ Ø¨Ø¹Ø¶ÙƒÙ… Ø§Ù„Ø¨Ø¹Ø¶ Ø®Ù„Ø§Ù„ Ø¹Ù…Ù„ÙŠØ© Ø§Ù„ØªÙ‚Ø¯ÙŠÙ….", null, false, false, 200, "DAAD Scholarship Applicants", "Ù…ØªÙ‚Ø¯Ù…Ùˆ Ù…Ù†Ø­Ø© DAAD", null, null },
                    { new Guid("60000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("10000000-0000-0000-0000-000000000001"), null, null, "Everything about studying in the United Kingdom: university selection, visa process, living costs, and scholarship opportunities.", "ÙƒÙ„ Ù…Ø§ ÙŠØªØ¹Ù„Ù‚ Ø¨Ø§Ù„Ø¯Ø±Ø§Ø³Ø© ÙÙŠ Ø§Ù„Ù…Ù…Ù„ÙƒØ© Ø§Ù„Ù…ØªØ­Ø¯Ø©: Ø§Ø®ØªÙŠØ§Ø± Ø§Ù„Ø¬Ø§Ù…Ø¹Ø© ÙˆØ¥Ø¬Ø±Ø§Ø¡Ø§Øª Ø§Ù„ØªØ£Ø´ÙŠØ±Ø© ÙˆØªÙƒØ§Ù„ÙŠÙ Ø§Ù„Ù…Ø¹ÙŠØ´Ø© ÙˆÙØ±Øµ Ø§Ù„Ù…Ù†Ø­ Ø§Ù„Ø¯Ø±Ø§Ø³ÙŠØ©.", null, false, false, 500, "Study in the UK", "Ø§Ù„Ø¯Ø±Ø§Ø³Ø© ÙÙŠ Ø¨Ø±ÙŠØ·Ø§Ù†ÙŠØ§", null, null }
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
                name: "IX_Groups_IsDeleted",
                table: "Groups",
                column: "IsDeleted");

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
                name: "IX_Likes_UserId",
                table: "Likes",
                column: "UserId");

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
                name: "IX_Posts_IsDeleted",
                table: "Posts",
                column: "IsDeleted");
        }
    }
}
