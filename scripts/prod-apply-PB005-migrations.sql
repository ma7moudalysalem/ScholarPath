/*
  ONE-TIME PRODUCTION FIX — apply the three PB-005 migrations that the deployed
  API expects but that never reached the prod Azure SQL database (so every query
  touching UserProfiles / the new tables returns HTTP 500).

  Migrations applied here:
    #27  20260522033457_AddCompanyReviewRequests_PB005   (new CompanyReviewRequests table)
    #29  20260522153032_AddCompanyLowRatingFields_PB005R (new UserProfiles columns)
    #32  20260522201500_AddCommunityBookmarksAndTags     (new Forum* tables)

  HOW TO RUN (no secrets needed):
    Azure Portal -> your SQL Database -> Query editor (preview) -> sign in with
    your Azure account -> paste this whole file -> Run.

  SAFE TO RE-RUN: every statement is guarded by object/column existence AND the
  EF __EFMigrationsHistory table, so already-applied pieces are skipped. After it
  runs, the API's startup migration sees these as applied and the 500s clear.
*/
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ── #27  AddCompanyReviewRequests_PB005 ──────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260522033457_AddCompanyReviewRequests_PB005')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'CompanyReviewRequests')
    BEGIN
        CREATE TABLE [CompanyReviewRequests] (
            [Id] uniqueidentifier NOT NULL,
            [StudentId] uniqueidentifier NOT NULL,
            [CompanyId] uniqueidentifier NOT NULL,
            [ScholarshipId] uniqueidentifier NOT NULL,
            [ApplicationTrackerId] uniqueidentifier NULL,
            [PaymentId] uniqueidentifier NULL,
            [Status] nvarchar(32) NOT NULL,
            [ReviewFeeUsdSnapshot] decimal(10,2) NOT NULL,
            [Currency] nvarchar(8) NOT NULL,
            [SubmittedAt] datetimeoffset NULL,
            [AcceptedAt] datetimeoffset NULL,
            [RejectedAt] datetimeoffset NULL,
            [CompletedAt] datetimeoffset NULL,
            [ClosedAt] datetimeoffset NULL,
            [CancelledAt] datetimeoffset NULL,
            [ExpiredAt] datetimeoffset NULL,
            [PendingExpiresAt] datetimeoffset NULL,
            [CancelReason] nvarchar(500) NULL,
            [RejectReason] nvarchar(500) NULL,
            [IsDeleted] bit NOT NULL,
            [DeletedAt] datetimeoffset NULL,
            [DeletedByUserId] uniqueidentifier NULL,
            [CreatedAt] datetimeoffset NOT NULL,
            [CreatedByUserId] uniqueidentifier NULL,
            [UpdatedAt] datetimeoffset NULL,
            [UpdatedByUserId] uniqueidentifier NULL,
            [RowVersion] rowversion NULL,
            CONSTRAINT [PK_CompanyReviewRequests] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_CompanyReviewRequests_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE SET NULL,
            CONSTRAINT [FK_CompanyReviewRequests_Scholarships_ScholarshipId] FOREIGN KEY ([ScholarshipId]) REFERENCES [Scholarships] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_CompanyReviewRequests_Users_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
            CONSTRAINT [FK_CompanyReviewRequests_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyReviewRequests_CompanyId_Status' AND object_id = OBJECT_ID(N'CompanyReviewRequests'))
        CREATE INDEX [IX_CompanyReviewRequests_CompanyId_Status] ON [CompanyReviewRequests] ([CompanyId], [Status]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyReviewRequests_PaymentId' AND object_id = OBJECT_ID(N'CompanyReviewRequests'))
        CREATE INDEX [IX_CompanyReviewRequests_PaymentId] ON [CompanyReviewRequests] ([PaymentId]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyReviewRequests_ScholarshipId' AND object_id = OBJECT_ID(N'CompanyReviewRequests'))
        CREATE INDEX [IX_CompanyReviewRequests_ScholarshipId] ON [CompanyReviewRequests] ([ScholarshipId]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyReviewRequests_StudentId_Status' AND object_id = OBJECT_ID(N'CompanyReviewRequests'))
        CREATE INDEX [IX_CompanyReviewRequests_StudentId_Status] ON [CompanyReviewRequests] ([StudentId], [Status]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CompanyReviewRequests_Student_Scholarship_Active' AND object_id = OBJECT_ID(N'CompanyReviewRequests'))
        EXEC(N'CREATE UNIQUE INDEX [UX_CompanyReviewRequests_Student_Scholarship_Active] ON [CompanyReviewRequests] ([StudentId], [ScholarshipId]) WHERE [Status] IN (''Draft'', ''Submitted'', ''Pending'', ''UnderReview'')');

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260522033457_AddCompanyReviewRequests_PB005', N'10.0.6');
END;
GO

-- ── #29  AddCompanyLowRatingFields_PB005R ────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260522153032_AddCompanyLowRatingFields_PB005R')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'UserProfiles') AND name = N'CompanyAverageRating')
        ALTER TABLE [UserProfiles] ADD [CompanyAverageRating] decimal(3,2) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'UserProfiles') AND name = N'CompanyLowRatingFlaggedAt')
        ALTER TABLE [UserProfiles] ADD [CompanyLowRatingFlaggedAt] datetimeoffset NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'UserProfiles') AND name = N'CompanyReviewCount')
        ALTER TABLE [UserProfiles] ADD [CompanyReviewCount] int NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserProfiles_CompanyLowRatingFlagged' AND object_id = OBJECT_ID(N'UserProfiles'))
        EXEC(N'CREATE INDEX [IX_UserProfiles_CompanyLowRatingFlagged] ON [UserProfiles] ([CompanyLowRatingFlaggedAt]) WHERE [CompanyLowRatingFlaggedAt] IS NOT NULL');

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260522153032_AddCompanyLowRatingFields_PB005R', N'10.0.6');
END;
GO

-- ── #32  AddCommunityBookmarksAndTags ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260522201500_AddCommunityBookmarksAndTags')
BEGIN
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

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260522201500_AddCommunityBookmarksAndTags', N'10.0.6');
END;
GO

-- Verify (optional): should return the three migration ids.
SELECT [MigrationId] FROM [__EFMigrationsHistory]
WHERE [MigrationId] IN (
    N'20260522033457_AddCompanyReviewRequests_PB005',
    N'20260522153032_AddCompanyLowRatingFields_PB005R',
    N'20260522201500_AddCommunityBookmarksAndTags'
);
GO
