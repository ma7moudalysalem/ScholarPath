using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarPath.Infrastructure.Migrations
{
    /// <summary>
    /// Adds Community bookmarks (per-user saved posts) and lightweight tags
    /// (ForumTag + ForumPostTag join). Idempotent — safe to re-run.
    /// </summary>
    public partial class AddCommunityBookmarksAndTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ForumBookmarks')
BEGIN
    CREATE TABLE [ForumBookmarks] (
        [Id] uniqueidentifier NOT NULL,
        [ForumPostId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ForumBookmarks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ForumBookmarks_ForumPosts_ForumPostId]
            FOREIGN KEY ([ForumPostId]) REFERENCES [ForumPosts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ForumBookmarks_ForumPostId_UserId' AND object_id = OBJECT_ID(N'ForumBookmarks'))
    CREATE UNIQUE INDEX [IX_ForumBookmarks_ForumPostId_UserId] ON [ForumBookmarks] ([ForumPostId], [UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ForumBookmarks_UserId' AND object_id = OBJECT_ID(N'ForumBookmarks'))
    CREATE INDEX [IX_ForumBookmarks_UserId] ON [ForumBookmarks] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ForumTags')
BEGIN
    CREATE TABLE [ForumTags] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(30) NOT NULL,
        [Slug] nvarchar(30) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ForumTags] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ForumTags_Slug' AND object_id = OBJECT_ID(N'ForumTags'))
    CREATE UNIQUE INDEX [IX_ForumTags_Slug] ON [ForumTags] ([Slug]);

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ForumPostTags')
BEGIN
    CREATE TABLE [ForumPostTags] (
        [ForumPostId] uniqueidentifier NOT NULL,
        [ForumTagId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ForumPostTags] PRIMARY KEY ([ForumPostId], [ForumTagId]),
        CONSTRAINT [FK_ForumPostTags_ForumPosts_ForumPostId]
            FOREIGN KEY ([ForumPostId]) REFERENCES [ForumPosts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ForumPostTags_ForumTags_ForumTagId]
            FOREIGN KEY ([ForumTagId]) REFERENCES [ForumTags] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ForumPostTags_ForumTagId' AND object_id = OBJECT_ID(N'ForumPostTags'))
    CREATE INDEX [IX_ForumPostTags_ForumTagId] ON [ForumPostTags] ([ForumTagId]);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ForumPostTags') DROP TABLE [ForumPostTags];
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ForumBookmarks') DROP TABLE [ForumBookmarks];
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ForumTags') DROP TABLE [ForumTags];
");
        }
    }
}
