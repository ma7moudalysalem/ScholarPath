IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AiInteractions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Feature] nvarchar(24) NOT NULL,
    [Provider] nvarchar(16) NOT NULL,
    [ModelName] nvarchar(100) NULL,
    [SessionId] nvarchar(128) NULL,
    [PromptText] nvarchar(max) NOT NULL,
    [ResponseText] nvarchar(max) NOT NULL,
    [PromptTokens] int NOT NULL,
    [CompletionTokens] int NOT NULL,
    [CostUsd] decimal(14,6) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [StartedAt] datetimeoffset NOT NULL,
    [CompletedAt] datetimeoffset NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_AiInteractions] PRIMARY KEY ([Id])
);

CREATE TABLE [AuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [ActorUserId] uniqueidentifier NULL,
    [Action] nvarchar(32) NOT NULL,
    [TargetType] nvarchar(100) NOT NULL,
    [TargetId] uniqueidentifier NULL,
    [BeforeJson] nvarchar(max) NULL,
    [AfterJson] nvarchar(max) NULL,
    [IpAddress] nvarchar(64) NULL,
    [UserAgent] nvarchar(512) NULL,
    [OccurredAt] datetimeoffset NOT NULL,
    [CorrelationId] nvarchar(128) NULL,
    [Summary] nvarchar(2000) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [Categories] (
    [Id] uniqueidentifier NOT NULL,
    [NameEn] nvarchar(100) NOT NULL,
    [NameAr] nvarchar(100) NOT NULL,
    [Slug] nvarchar(120) NOT NULL,
    [DescriptionEn] nvarchar(1000) NULL,
    [DescriptionAr] nvarchar(1000) NULL,
    [IconKey] nvarchar(64) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [CompanyReviewPayments] (
    [Id] uniqueidentifier NOT NULL,
    [ApplicationTrackerId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [AmountUsd] decimal(14,2) NOT NULL,
    [ProfitShareAmountUsd] decimal(14,2) NOT NULL,
    [PayeeAmountUsd] decimal(14,2) NOT NULL,
    [StripePaymentIntentId] nvarchar(256) NOT NULL,
    [IdempotencyKey] nvarchar(128) NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [CapturedAt] datetimeoffset NULL,
    [RefundedAmountUsd] decimal(14,2) NULL,
    [RefundReason] nvarchar(500) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_CompanyReviewPayments] PRIMARY KEY ([Id])
);

CREATE TABLE [Conversations] (
    [Id] uniqueidentifier NOT NULL,
    [ParticipantOneId] uniqueidentifier NOT NULL,
    [ParticipantTwoId] uniqueidentifier NOT NULL,
    [LastMessageAt] datetimeoffset NULL,
    [LastMessageId] uniqueidentifier NULL,
    [IsArchivedForParticipantOne] bit NOT NULL,
    [IsArchivedForParticipantTwo] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
);

CREATE TABLE [ExpertiseTags] (
    [Id] uniqueidentifier NOT NULL,
    [NameEn] nvarchar(100) NOT NULL,
    [NameAr] nvarchar(100) NOT NULL,
    [Slug] nvarchar(120) NOT NULL,
    [Category] nvarchar(50) NULL,
    CONSTRAINT [PK_ExpertiseTags] PRIMARY KEY ([Id])
);

CREATE TABLE [ForumCategories] (
    [Id] uniqueidentifier NOT NULL,
    [NameEn] nvarchar(100) NOT NULL,
    [NameAr] nvarchar(100) NOT NULL,
    [Slug] nvarchar(120) NOT NULL,
    [DescriptionEn] nvarchar(500) NULL,
    [DescriptionAr] nvarchar(500) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_ForumCategories] PRIMARY KEY ([Id])
);

CREATE TABLE [NotificationPreferences] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Type] nvarchar(48) NOT NULL,
    [Channel] nvarchar(16) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY ([Id])
);

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [RecipientUserId] uniqueidentifier NOT NULL,
    [Type] nvarchar(48) NOT NULL,
    [Channel] nvarchar(16) NOT NULL,
    [TitleEn] nvarchar(300) NOT NULL,
    [TitleAr] nvarchar(300) NOT NULL,
    [BodyEn] nvarchar(2000) NOT NULL,
    [BodyAr] nvarchar(2000) NOT NULL,
    [DeepLink] nvarchar(2048) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsRead] bit NOT NULL,
    [ReadAt] datetimeoffset NULL,
    [Priority] int NOT NULL,
    [IdempotencyKey] nvarchar(128) NULL,
    [DispatchedAt] datetimeoffset NULL,
    [DispatchSucceeded] bit NOT NULL,
    [DispatchError] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
);

CREATE TABLE [Payments] (
    [Id] uniqueidentifier NOT NULL,
    [Type] nvarchar(32) NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [AmountCents] bigint NOT NULL,
    [Currency] nvarchar(8) NOT NULL,
    [ProfitShareAmountCents] bigint NOT NULL,
    [PayeeAmountCents] bigint NOT NULL,
    [RefundedAmountCents] bigint NOT NULL,
    [PayerUserId] uniqueidentifier NOT NULL,
    [PayeeUserId] uniqueidentifier NULL,
    [StripePaymentIntentId] nvarchar(256) NULL,
    [StripeChargeId] nvarchar(256) NULL,
    [IdempotencyKey] nvarchar(128) NOT NULL,
    [RelatedBookingId] uniqueidentifier NULL,
    [RelatedApplicationId] uniqueidentifier NULL,
    [HeldAt] datetimeoffset NULL,
    [CapturedAt] datetimeoffset NULL,
    [RefundedAt] datetimeoffset NULL,
    [RefundReason] nvarchar(500) NULL,
    [FailureReason] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id])
);

CREATE TABLE [Payouts] (
    [Id] uniqueidentifier NOT NULL,
    [PayeeUserId] uniqueidentifier NOT NULL,
    [AmountCents] bigint NOT NULL,
    [Currency] nvarchar(8) NOT NULL,
    [Status] nvarchar(16) NOT NULL,
    [StripePayoutId] nvarchar(256) NULL,
    [StripeConnectAccountId] nvarchar(256) NULL,
    [InitiatedAt] datetimeoffset NULL,
    [PaidAt] datetimeoffset NULL,
    [FailureReason] nvarchar(500) NULL,
    [IncludedPaymentIdsJson] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Payouts] PRIMARY KEY ([Id])
);

CREATE TABLE [ProfitShareConfigs] (
    [Id] uniqueidentifier NOT NULL,
    [PaymentType] nvarchar(32) NOT NULL,
    [Percentage] decimal(5,4) NOT NULL,
    [EffectiveFrom] datetimeoffset NOT NULL,
    [EffectiveTo] datetimeoffset NULL,
    [SetByAdminId] uniqueidentifier NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_ProfitShareConfigs] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL,
    [Description] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [StripeWebhookEvents] (
    [Id] uniqueidentifier NOT NULL,
    [StripeEventId] nvarchar(256) NOT NULL,
    [EventType] nvarchar(100) NOT NULL,
    [RawPayload] nvarchar(max) NOT NULL,
    [ReceivedAt] datetimeoffset NOT NULL,
    [ProcessedAt] datetimeoffset NULL,
    [IsProcessed] bit NOT NULL,
    [ProcessingError] nvarchar(2000) NULL,
    [ProcessingAttempts] int NOT NULL,
    CONSTRAINT [PK_StripeWebhookEvents] PRIMARY KEY ([Id])
);

CREATE TABLE [SuccessStories] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NULL,
    [AuthorDisplayName] nvarchar(200) NOT NULL,
    [AuthorImageUrl] nvarchar(2048) NULL,
    [HeadlineEn] nvarchar(300) NOT NULL,
    [HeadlineAr] nvarchar(300) NOT NULL,
    [BodyEn] nvarchar(4000) NOT NULL,
    [BodyAr] nvarchar(4000) NOT NULL,
    [ScholarshipNameEn] nvarchar(300) NULL,
    [ScholarshipNameAr] nvarchar(300) NULL,
    [CountryCode] nvarchar(8) NULL,
    [IsApproved] bit NOT NULL,
    [IsFeatured] bit NOT NULL,
    [FeaturedOrder] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_SuccessStories] PRIMARY KEY ([Id])
);

CREATE TABLE [UserBlocks] (
    [Id] uniqueidentifier NOT NULL,
    [BlockerId] uniqueidentifier NOT NULL,
    [BlockedUserId] uniqueidentifier NOT NULL,
    [BlockedAt] datetimeoffset NOT NULL,
    [Reason] nvarchar(500) NULL,
    CONSTRAINT [PK_UserBlocks] PRIMARY KEY ([Id])
);

CREATE TABLE [UserDataRequests] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Type] nvarchar(16) NOT NULL,
    [Status] nvarchar(16) NOT NULL,
    [RequestedAt] datetimeoffset NOT NULL,
    [ScheduledProcessAt] datetimeoffset NULL,
    [CompletedAt] datetimeoffset NULL,
    [CancelledAt] datetimeoffset NULL,
    [DownloadUrl] nvarchar(2048) NULL,
    [DownloadExpiresAt] datetimeoffset NULL,
    [FailureReason] nvarchar(2000) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_UserDataRequests] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [ProfileImageUrl] nvarchar(2048) NULL,
    [AccountStatus] nvarchar(32) NOT NULL,
    [IsOnboardingComplete] bit NOT NULL,
    [LastLoginAt] datetimeoffset NULL,
    [PreferredLanguage] nvarchar(8) NULL,
    [CountryOfResidence] nvarchar(64) NULL,
    [ActiveRole] nvarchar(32) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Messages] (
    [Id] uniqueidentifier NOT NULL,
    [ConversationId] uniqueidentifier NOT NULL,
    [SenderId] uniqueidentifier NOT NULL,
    [Body] nvarchar(4000) NOT NULL,
    [SentAt] datetimeoffset NOT NULL,
    [ReadAt] datetimeoffset NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Availabilities] (
    [Id] uniqueidentifier NOT NULL,
    [ConsultantId] uniqueidentifier NOT NULL,
    [DayOfWeek] int NULL,
    [StartTime] time NULL,
    [EndTime] time NULL,
    [SpecificStartAt] datetimeoffset NULL,
    [SpecificEndAt] datetimeoffset NULL,
    [Timezone] nvarchar(64) NOT NULL,
    [IsRecurring] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Availabilities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Availabilities_Users_ConsultantId] FOREIGN KEY ([ConsultantId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Bookings] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [ConsultantId] uniqueidentifier NOT NULL,
    [AvailabilityId] uniqueidentifier NULL,
    [ScheduledStartAt] datetimeoffset NOT NULL,
    [ScheduledEndAt] datetimeoffset NOT NULL,
    [DurationMinutes] int NOT NULL,
    [PriceUsd] decimal(10,2) NOT NULL,
    [MeetingUrl] nvarchar(2048) NULL,
    [Status] nvarchar(24) NOT NULL,
    [RequestedAt] datetimeoffset NULL,
    [ConfirmedAt] datetimeoffset NULL,
    [CancelledAt] datetimeoffset NULL,
    [CompletedAt] datetimeoffset NULL,
    [CancellationReason] nvarchar(500) NULL,
    [CancelledByUserId] uniqueidentifier NULL,
    [PaymentId] uniqueidentifier NULL,
    [StripePaymentIntentId] nvarchar(256) NULL,
    [IsNoShowStudent] bit NOT NULL,
    [IsNoShowConsultant] bit NOT NULL,
    [NoShowMarkedAt] datetimeoffset NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bookings_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Bookings_Users_ConsultantId] FOREIGN KEY ([ConsultantId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bookings_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ForumPosts] (
    [Id] uniqueidentifier NOT NULL,
    [AuthorId] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NULL,
    [ParentPostId] uniqueidentifier NULL,
    [Title] nvarchar(500) NULL,
    [BodyMarkdown] nvarchar(max) NOT NULL,
    [ModerationStatus] nvarchar(24) NOT NULL,
    [UpvoteCount] int NOT NULL,
    [DownvoteCount] int NOT NULL,
    [FlagCount] int NOT NULL,
    [ReplyCount] int NOT NULL,
    [IsAutoHidden] bit NOT NULL,
    [AutoHiddenAt] datetimeoffset NULL,
    [ModeratedByAdminId] uniqueidentifier NULL,
    [ModeratedAt] datetimeoffset NULL,
    [ModerationNote] nvarchar(1000) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_ForumPosts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ForumPosts_ForumCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ForumCategories] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ForumPosts_ForumPosts_ParentPostId] FOREIGN KEY ([ParentPostId]) REFERENCES [ForumPosts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ForumPosts_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoginAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [UserId] uniqueidentifier NULL,
    [Succeeded] bit NOT NULL,
    [FailureReason] nvarchar(256) NULL,
    [OccurredAt] datetimeoffset NOT NULL,
    [IpAddress] nvarchar(64) NULL,
    [UserAgent] nvarchar(512) NULL,
    [ApplicationUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_LoginAttempts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoginAttempts_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(512) NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedAt] datetimeoffset NULL,
    [RevokedReason] nvarchar(256) NULL,
    [ReplacedByTokenId] uniqueidentifier NULL,
    [IpAddress] nvarchar(64) NULL,
    [UserAgent] nvarchar(512) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Resources] (
    [Id] uniqueidentifier NOT NULL,
    [TitleEn] nvarchar(300) NOT NULL,
    [TitleAr] nvarchar(300) NOT NULL,
    [Slug] nvarchar(320) NOT NULL,
    [DescriptionEn] nvarchar(2000) NULL,
    [DescriptionAr] nvarchar(2000) NULL,
    [ContentMarkdownEn] nvarchar(max) NULL,
    [ContentMarkdownAr] nvarchar(max) NULL,
    [ExternalLinkUrl] nvarchar(2048) NULL,
    [CoverImageUrl] nvarchar(2048) NULL,
    [AuthorUserId] uniqueidentifier NOT NULL,
    [AuthorRole] nvarchar(32) NOT NULL,
    [Type] nvarchar(24) NOT NULL,
    [Status] nvarchar(24) NOT NULL,
    [CategorySlug] nvarchar(120) NULL,
    [TagsJson] nvarchar(max) NULL,
    [IsFeatured] bit NOT NULL,
    [FeaturedOrder] int NOT NULL,
    [PublishedAt] datetimeoffset NULL,
    [ReviewedAt] datetimeoffset NULL,
    [ReviewedByAdminId] uniqueidentifier NULL,
    [RejectionReason] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [AuthorId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Resources] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Resources_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Scholarships] (
    [Id] uniqueidentifier NOT NULL,
    [TitleEn] nvarchar(300) NOT NULL,
    [TitleAr] nvarchar(300) NOT NULL,
    [DescriptionEn] nvarchar(4000) NOT NULL,
    [DescriptionAr] nvarchar(4000) NOT NULL,
    [Slug] nvarchar(320) NOT NULL,
    [CategoryId] uniqueidentifier NULL,
    [OwnerCompanyId] uniqueidentifier NULL,
    [CreatedByAdminId] uniqueidentifier NULL,
    [Mode] nvarchar(16) NOT NULL,
    [ExternalApplicationUrl] nvarchar(2048) NULL,
    [Status] nvarchar(16) NOT NULL,
    [Deadline] datetimeoffset NOT NULL,
    [OpenedAt] datetimeoffset NULL,
    [ArchivedAt] datetimeoffset NULL,
    [IsFeatured] bit NOT NULL,
    [FeaturedOrder] int NOT NULL,
    [FundingType] nvarchar(24) NOT NULL,
    [FundingAmountUsd] decimal(14,2) NULL,
    [Currency] nvarchar(8) NULL,
    [TargetLevel] nvarchar(24) NOT NULL,
    [TargetCountriesJson] nvarchar(max) NULL,
    [EligibilityRequirementsEn] nvarchar(4000) NULL,
    [EligibilityRequirementsAr] nvarchar(4000) NULL,
    [TagsJson] nvarchar(max) NULL,
    [ApplicationFormSchemaJson] nvarchar(max) NULL,
    [RequiredDocumentsJson] nvarchar(max) NULL,
    [ReviewFeeUsd] decimal(10,2) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Scholarships] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Scholarships_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Scholarships_Users_OwnerCompanyId] FOREIGN KEY ([OwnerCompanyId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [UpgradeRequests] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Target] nvarchar(16) NOT NULL,
    [Status] nvarchar(16) NOT NULL,
    [Reason] nvarchar(2000) NULL,
    [ReviewerNotes] nvarchar(2000) NULL,
    [ReviewedByAdminId] uniqueidentifier NULL,
    [ReviewedAt] datetimeoffset NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_UpgradeRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UpgradeRequests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserProfiles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Biography] nvarchar(4000) NULL,
    [DateOfBirth] date NULL,
    [Nationality] nvarchar(64) NULL,
    [LinkedInUrl] nvarchar(2048) NULL,
    [WebsiteUrl] nvarchar(2048) NULL,
    [Timezone] nvarchar(64) NULL,
    [AcademicLevel] nvarchar(32) NULL,
    [FieldOfStudy] nvarchar(200) NULL,
    [CurrentInstitution] nvarchar(200) NULL,
    [Gpa] decimal(4,2) NULL,
    [GpaScale] nvarchar(max) NULL,
    [PreferredCountriesJson] nvarchar(max) NULL,
    [PreferredFieldsJson] nvarchar(max) NULL,
    [OrganizationLegalName] nvarchar(max) NULL,
    [OrganizationRegistrationNumber] nvarchar(max) NULL,
    [OrganizationWebsite] nvarchar(max) NULL,
    [OrganizationVerificationStatus] nvarchar(max) NULL,
    [OrganizationVerifiedAt] datetimeoffset NULL,
    [SessionFeeUsd] decimal(10,2) NULL,
    [SessionDurationMinutes] int NULL,
    [ExpertiseTagsJson] nvarchar(max) NULL,
    [LanguagesJson] nvarchar(max) NULL,
    [ConsultantVerifiedAt] datetimeoffset NULL,
    [ProfileCompletenessPercent] int NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ConsultantReviews] (
    [Id] uniqueidentifier NOT NULL,
    [BookingId] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [ConsultantId] uniqueidentifier NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(2000) NULL,
    [IsHiddenByAdmin] bit NOT NULL,
    [AdminNote] nvarchar(1000) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_ConsultantReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ConsultantReviews_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ConsultantReviews_Users_ConsultantId] FOREIGN KEY ([ConsultantId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ConsultantReviews_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ForumFlags] (
    [Id] uniqueidentifier NOT NULL,
    [ForumPostId] uniqueidentifier NOT NULL,
    [FlaggedByUserId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(200) NOT NULL,
    [AdditionalDetails] nvarchar(1000) NULL,
    [FlaggedAt] datetimeoffset NOT NULL,
    [IsValid] bit NOT NULL,
    CONSTRAINT [PK_ForumFlags] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ForumFlags_ForumPosts_ForumPostId] FOREIGN KEY ([ForumPostId]) REFERENCES [ForumPosts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ForumPostAttachments] (
    [Id] uniqueidentifier NOT NULL,
    [ForumPostId] uniqueidentifier NOT NULL,
    [FileName] nvarchar(512) NOT NULL,
    [BlobUrl] nvarchar(2048) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    CONSTRAINT [PK_ForumPostAttachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ForumPostAttachments_ForumPosts_ForumPostId] FOREIGN KEY ([ForumPostId]) REFERENCES [ForumPosts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ForumVotes] (
    [Id] uniqueidentifier NOT NULL,
    [ForumPostId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [VoteType] nvarchar(8) NOT NULL,
    [VotedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ForumVotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ForumVotes_ForumPosts_ForumPostId] FOREIGN KEY ([ForumPostId]) REFERENCES [ForumPosts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ResourceBookmarks] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ResourceId] uniqueidentifier NOT NULL,
    [BookmarkedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ResourceBookmarks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ResourceBookmarks_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ResourceChapters] (
    [Id] uniqueidentifier NOT NULL,
    [ResourceId] uniqueidentifier NOT NULL,
    [TitleEn] nvarchar(300) NOT NULL,
    [TitleAr] nvarchar(300) NOT NULL,
    [ContentMarkdownEn] nvarchar(max) NULL,
    [ContentMarkdownAr] nvarchar(max) NULL,
    [SortOrder] int NOT NULL,
    [EstimatedReadMinutes] int NOT NULL,
    CONSTRAINT [PK_ResourceChapters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ResourceChapters_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ResourceProgress] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ResourceId] uniqueidentifier NOT NULL,
    [ChaptersCompletedCount] int NOT NULL,
    [LastAccessedAt] datetimeoffset NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_ResourceProgress] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ResourceProgress_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Applications] (
    [Id] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [ScholarshipId] uniqueidentifier NOT NULL,
    [Mode] nvarchar(16) NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [FormDataJson] nvarchar(max) NULL,
    [AttachedDocumentsJson] nvarchar(max) NULL,
    [ExternalTrackingUrl] nvarchar(2048) NULL,
    [ExternalReferenceId] nvarchar(256) NULL,
    [SubmittedAt] datetimeoffset NULL,
    [WithdrawnAt] datetimeoffset NULL,
    [ReviewStartedAt] datetimeoffset NULL,
    [DecisionAt] datetimeoffset NULL,
    [DecisionReason] nvarchar(2000) NULL,
    [IsReadOnly] bit NOT NULL,
    [NextReminderAt] datetimeoffset NULL,
    [PersonalNotes] nvarchar(4000) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Applications_Scholarships_ScholarshipId] FOREIGN KEY ([ScholarshipId]) REFERENCES [Scholarships] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Applications_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SavedScholarships] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ScholarshipId] uniqueidentifier NOT NULL,
    [SavedAt] datetimeoffset NOT NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_SavedScholarships] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SavedScholarships_Scholarships_ScholarshipId] FOREIGN KEY ([ScholarshipId]) REFERENCES [Scholarships] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ScholarshipChildren] (
    [Id] uniqueidentifier NOT NULL,
    [ScholarshipId] uniqueidentifier NOT NULL,
    [ChildType] nvarchar(50) NOT NULL,
    [KeyEn] nvarchar(300) NOT NULL,
    [KeyAr] nvarchar(300) NULL,
    [ValueEn] nvarchar(2000) NULL,
    [ValueAr] nvarchar(2000) NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_ScholarshipChildren] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ScholarshipChildren_Scholarships_ScholarshipId] FOREIGN KEY ([ScholarshipId]) REFERENCES [Scholarships] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UpgradeRequestFiles] (
    [Id] uniqueidentifier NOT NULL,
    [UpgradeRequestId] uniqueidentifier NOT NULL,
    [FileName] nvarchar(512) NOT NULL,
    [BlobUrl] nvarchar(2048) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [UploadedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_UpgradeRequestFiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UpgradeRequestFiles_UpgradeRequests_UpgradeRequestId] FOREIGN KEY ([UpgradeRequestId]) REFERENCES [UpgradeRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UpgradeRequestLinks] (
    [Id] uniqueidentifier NOT NULL,
    [UpgradeRequestId] uniqueidentifier NOT NULL,
    [Label] nvarchar(200) NOT NULL,
    [Url] nvarchar(2048) NOT NULL,
    CONSTRAINT [PK_UpgradeRequestLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UpgradeRequestLinks_UpgradeRequests_UpgradeRequestId] FOREIGN KEY ([UpgradeRequestId]) REFERENCES [UpgradeRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EducationEntries] (
    [Id] uniqueidentifier NOT NULL,
    [UserProfileId] uniqueidentifier NOT NULL,
    [InstitutionName] nvarchar(200) NOT NULL,
    [Degree] nvarchar(200) NOT NULL,
    [FieldOfStudy] nvarchar(200) NOT NULL,
    [StartDate] date NULL,
    [EndDate] date NULL,
    [Gpa] decimal(4,2) NULL,
    [Description] nvarchar(2000) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_EducationEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EducationEntries_UserProfiles_UserProfileId] FOREIGN KEY ([UserProfileId]) REFERENCES [UserProfiles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ResourceProgressChildren] (
    [Id] uniqueidentifier NOT NULL,
    [ResourceProgressId] uniqueidentifier NOT NULL,
    [ResourceChildId] uniqueidentifier NOT NULL,
    [IsCompleted] bit NOT NULL,
    [CompletedAt] datetimeoffset NULL,
    CONSTRAINT [PK_ResourceProgressChildren] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ResourceProgressChildren_ResourceProgress_ResourceProgressId] FOREIGN KEY ([ResourceProgressId]) REFERENCES [ResourceProgress] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ApplicationChildren] (
    [Id] uniqueidentifier NOT NULL,
    [ApplicationTrackerId] uniqueidentifier NOT NULL,
    [ChildType] nvarchar(50) NOT NULL,
    [Title] nvarchar(300) NULL,
    [Content] nvarchar(4000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [OccurredAt] datetimeoffset NOT NULL,
    [ActorUserId] uniqueidentifier NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_ApplicationChildren] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApplicationChildren_Applications_ApplicationTrackerId] FOREIGN KEY ([ApplicationTrackerId]) REFERENCES [Applications] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CompanyReviews] (
    [Id] uniqueidentifier NOT NULL,
    [ApplicationTrackerId] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(2000) NULL,
    [IsHiddenByAdmin] bit NOT NULL,
    [AdminNote] nvarchar(1000) NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_CompanyReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompanyReviews_Applications_ApplicationTrackerId] FOREIGN KEY ([ApplicationTrackerId]) REFERENCES [Applications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CompanyReviews_Users_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CompanyReviews_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AiInteractions_SessionId] ON [AiInteractions] ([SessionId]);

CREATE INDEX [IX_AiInteractions_UserId_StartedAt] ON [AiInteractions] ([UserId], [StartedAt]);

CREATE INDEX [IX_ApplicationChildren_ApplicationTrackerId_ChildType] ON [ApplicationChildren] ([ApplicationTrackerId], [ChildType]);

CREATE INDEX [IX_Applications_ScholarshipId] ON [Applications] ([ScholarshipId]);

CREATE INDEX [IX_Applications_Status] ON [Applications] ([Status]);

CREATE UNIQUE INDEX [IX_Applications_StudentId_ScholarshipId] ON [Applications] ([StudentId], [ScholarshipId]) WHERE [Status] <> 'Withdrawn' AND [Status] <> 'Rejected' AND [Status] <> 'Accepted';

CREATE INDEX [IX_AuditLogs_ActorUserId] ON [AuditLogs] ([ActorUserId]);

CREATE INDEX [IX_AuditLogs_OccurredAt] ON [AuditLogs] ([OccurredAt]);

CREATE INDEX [IX_AuditLogs_TargetType_TargetId] ON [AuditLogs] ([TargetType], [TargetId]);

CREATE INDEX [IX_Availabilities_ConsultantId_IsActive] ON [Availabilities] ([ConsultantId], [IsActive]);

CREATE INDEX [IX_Bookings_ConsultantId_ScheduledStartAt] ON [Bookings] ([ConsultantId], [ScheduledStartAt]);

CREATE INDEX [IX_Bookings_PaymentId] ON [Bookings] ([PaymentId]);

CREATE INDEX [IX_Bookings_Status] ON [Bookings] ([Status]);

CREATE INDEX [IX_Bookings_StudentId_Status] ON [Bookings] ([StudentId], [Status]);

CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);

CREATE UNIQUE INDEX [IX_CompanyReviewPayments_IdempotencyKey] ON [CompanyReviewPayments] ([IdempotencyKey]);

CREATE UNIQUE INDEX [IX_CompanyReviewPayments_StripePaymentIntentId] ON [CompanyReviewPayments] ([StripePaymentIntentId]);

CREATE UNIQUE INDEX [IX_CompanyReviews_ApplicationTrackerId] ON [CompanyReviews] ([ApplicationTrackerId]);

CREATE INDEX [IX_CompanyReviews_CompanyId_IsHiddenByAdmin_IsDeleted] ON [CompanyReviews] ([CompanyId], [IsHiddenByAdmin], [IsDeleted]);

CREATE INDEX [IX_CompanyReviews_StudentId] ON [CompanyReviews] ([StudentId]);

CREATE UNIQUE INDEX [IX_ConsultantReviews_BookingId] ON [ConsultantReviews] ([BookingId]);

CREATE INDEX [IX_ConsultantReviews_ConsultantId_IsHiddenByAdmin_IsDeleted] ON [ConsultantReviews] ([ConsultantId], [IsHiddenByAdmin], [IsDeleted]);

CREATE INDEX [IX_ConsultantReviews_StudentId] ON [ConsultantReviews] ([StudentId]);

CREATE INDEX [IX_Conversations_LastMessageAt] ON [Conversations] ([LastMessageAt]);

CREATE UNIQUE INDEX [IX_Conversations_ParticipantOneId_ParticipantTwoId] ON [Conversations] ([ParticipantOneId], [ParticipantTwoId]);

CREATE INDEX [IX_EducationEntries_UserProfileId] ON [EducationEntries] ([UserProfileId]);

CREATE UNIQUE INDEX [IX_ExpertiseTags_Slug] ON [ExpertiseTags] ([Slug]);

CREATE UNIQUE INDEX [IX_ForumCategories_Slug] ON [ForumCategories] ([Slug]);

CREATE UNIQUE INDEX [IX_ForumFlags_ForumPostId_FlaggedByUserId] ON [ForumFlags] ([ForumPostId], [FlaggedByUserId]);

CREATE INDEX [IX_ForumPostAttachments_ForumPostId] ON [ForumPostAttachments] ([ForumPostId]);

CREATE INDEX [IX_ForumPosts_AuthorId] ON [ForumPosts] ([AuthorId]);

CREATE INDEX [IX_ForumPosts_CategoryId_CreatedAt] ON [ForumPosts] ([CategoryId], [CreatedAt]);

CREATE INDEX [IX_ForumPosts_IsAutoHidden] ON [ForumPosts] ([IsAutoHidden]);

CREATE INDEX [IX_ForumPosts_ParentPostId] ON [ForumPosts] ([ParentPostId]);

CREATE UNIQUE INDEX [IX_ForumVotes_ForumPostId_UserId] ON [ForumVotes] ([ForumPostId], [UserId]);

CREATE INDEX [IX_LoginAttempts_ApplicationUserId] ON [LoginAttempts] ([ApplicationUserId]);

CREATE INDEX [IX_LoginAttempts_Email_OccurredAt] ON [LoginAttempts] ([Email], [OccurredAt]);

CREATE INDEX [IX_Messages_ConversationId_SentAt] ON [Messages] ([ConversationId], [SentAt]);

CREATE UNIQUE INDEX [IX_NotificationPreferences_UserId_Type_Channel] ON [NotificationPreferences] ([UserId], [Type], [Channel]);

CREATE INDEX [IX_Notifications_IdempotencyKey] ON [Notifications] ([IdempotencyKey]);

CREATE INDEX [IX_Notifications_RecipientUserId_IsRead_CreatedAt] ON [Notifications] ([RecipientUserId], [IsRead], [CreatedAt]);

CREATE UNIQUE INDEX [IX_Payments_IdempotencyKey] ON [Payments] ([IdempotencyKey]);

CREATE INDEX [IX_Payments_PayeeUserId_Status] ON [Payments] ([PayeeUserId], [Status]);

CREATE INDEX [IX_Payments_PayerUserId_Status] ON [Payments] ([PayerUserId], [Status]);

CREATE INDEX [IX_Payments_StripePaymentIntentId] ON [Payments] ([StripePaymentIntentId]);

CREATE INDEX [IX_Payouts_PayeeUserId_Status] ON [Payouts] ([PayeeUserId], [Status]);

CREATE INDEX [IX_ProfitShareConfigs_PaymentType_EffectiveTo] ON [ProfitShareConfigs] ([PaymentType], [EffectiveTo]);

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);

CREATE INDEX [IX_RefreshTokens_UserId_IsRevoked] ON [RefreshTokens] ([UserId], [IsRevoked]);

CREATE INDEX [IX_ResourceBookmarks_ResourceId] ON [ResourceBookmarks] ([ResourceId]);

CREATE UNIQUE INDEX [IX_ResourceBookmarks_UserId_ResourceId] ON [ResourceBookmarks] ([UserId], [ResourceId]);

CREATE INDEX [IX_ResourceChapters_ResourceId_SortOrder] ON [ResourceChapters] ([ResourceId], [SortOrder]);

CREATE INDEX [IX_ResourceProgress_ResourceId] ON [ResourceProgress] ([ResourceId]);

CREATE UNIQUE INDEX [IX_ResourceProgress_UserId_ResourceId] ON [ResourceProgress] ([UserId], [ResourceId]);

CREATE UNIQUE INDEX [IX_ResourceProgressChildren_ResourceProgressId_ResourceChildId] ON [ResourceProgressChildren] ([ResourceProgressId], [ResourceChildId]);

CREATE INDEX [IX_Resources_AuthorId] ON [Resources] ([AuthorId]);

CREATE INDEX [IX_Resources_AuthorUserId] ON [Resources] ([AuthorUserId]);

CREATE UNIQUE INDEX [IX_Resources_Slug] ON [Resources] ([Slug]);

CREATE INDEX [IX_Resources_Status_IsFeatured] ON [Resources] ([Status], [IsFeatured]);

CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_SavedScholarships_ScholarshipId] ON [SavedScholarships] ([ScholarshipId]);

CREATE UNIQUE INDEX [IX_SavedScholarships_UserId_ScholarshipId] ON [SavedScholarships] ([UserId], [ScholarshipId]);

CREATE INDEX [IX_ScholarshipChildren_ScholarshipId_ChildType] ON [ScholarshipChildren] ([ScholarshipId], [ChildType]);

CREATE INDEX [IX_Scholarships_CategoryId] ON [Scholarships] ([CategoryId]);

CREATE INDEX [IX_Scholarships_Deadline] ON [Scholarships] ([Deadline]);

CREATE INDEX [IX_Scholarships_IsFeatured] ON [Scholarships] ([IsFeatured]);

CREATE INDEX [IX_Scholarships_Mode] ON [Scholarships] ([Mode]);

CREATE INDEX [IX_Scholarships_OwnerCompanyId] ON [Scholarships] ([OwnerCompanyId]);

CREATE UNIQUE INDEX [IX_Scholarships_Slug] ON [Scholarships] ([Slug]);

CREATE INDEX [IX_Scholarships_Status] ON [Scholarships] ([Status]);

CREATE INDEX [IX_Scholarships_Status_Deadline] ON [Scholarships] ([Status], [Deadline]);

CREATE INDEX [IX_StripeWebhookEvents_IsProcessed_ReceivedAt] ON [StripeWebhookEvents] ([IsProcessed], [ReceivedAt]);

CREATE UNIQUE INDEX [IX_StripeWebhookEvents_StripeEventId] ON [StripeWebhookEvents] ([StripeEventId]);

CREATE INDEX [IX_SuccessStories_IsApproved_IsFeatured] ON [SuccessStories] ([IsApproved], [IsFeatured]);

CREATE INDEX [IX_UpgradeRequestFiles_UpgradeRequestId] ON [UpgradeRequestFiles] ([UpgradeRequestId]);

CREATE INDEX [IX_UpgradeRequestLinks_UpgradeRequestId] ON [UpgradeRequestLinks] ([UpgradeRequestId]);

CREATE INDEX [IX_UpgradeRequests_UserId_Status] ON [UpgradeRequests] ([UserId], [Status]);

CREATE UNIQUE INDEX [IX_UserBlocks_BlockerId_BlockedUserId] ON [UserBlocks] ([BlockerId], [BlockedUserId]);

CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);

CREATE INDEX [IX_UserDataRequests_UserId_Type_Status] ON [UserDataRequests] ([UserId], [Type], [Status]);

CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);

CREATE UNIQUE INDEX [IX_UserProfiles_UserId] ON [UserProfiles] ([UserId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);

CREATE INDEX [IX_Users_AccountStatus] ON [Users] ([AccountStatus]);

CREATE INDEX [IX_Users_IsOnboardingComplete] ON [Users] ([IsOnboardingComplete]);

CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260418225113_InitialSchema', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Bookings] ADD [ExpiredAt] datetimeoffset NULL;

ALTER TABLE [Bookings] ADD [RejectedAt] datetimeoffset NULL;

CREATE INDEX [IX_Bookings_AvailabilityId] ON [Bookings] ([AvailabilityId]);

CREATE INDEX [IX_Bookings_StripePaymentIntentId] ON [Bookings] ([StripePaymentIntentId]);

CREATE INDEX [IX_Availabilities_ConsultantId_DayOfWeek_StartTime_IsActive] ON [Availabilities] ([ConsultantId], [DayOfWeek], [StartTime], [IsActive]);

CREATE INDEX [IX_Availabilities_ConsultantId_SpecificStartAt_IsActive] ON [Availabilities] ([ConsultantId], [SpecificStartAt], [IsActive]);

ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_Availabilities_AvailabilityId] FOREIGN KEY ([AvailabilityId]) REFERENCES [Availabilities] ([Id]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260423053837_RefineConsultantBookingSchema', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [AiRedactionAuditSamples] (
    [Id] uniqueidentifier NOT NULL,
    [AiInteractionId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [RedactedPrompt] nvarchar(max) NOT NULL,
    [SampledAt] datetimeoffset NOT NULL,
    [Verdict] nvarchar(24) NULL,
    [ReviewerUserId] uniqueidentifier NULL,
    [ReviewedAt] datetimeoffset NULL,
    CONSTRAINT [PK_AiRedactionAuditSamples] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AiRedactionAuditSamples_AiInteractions_AiInteractionId] FOREIGN KEY ([AiInteractionId]) REFERENCES [AiInteractions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AiRedactionAuditSamples_Users_ReviewerUserId] FOREIGN KEY ([ReviewerUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AiRedactionAuditSamples_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RecommendationClickEvents] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ScholarshipId] uniqueidentifier NOT NULL,
    [AiInteractionId] uniqueidentifier NULL,
    [ClickedAt] datetimeoffset NOT NULL,
    [Source] nvarchar(16) NOT NULL,
    CONSTRAINT [PK_RecommendationClickEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecommendationClickEvents_AiInteractions_AiInteractionId] FOREIGN KEY ([AiInteractionId]) REFERENCES [AiInteractions] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RecommendationClickEvents_Scholarships_ScholarshipId] FOREIGN KEY ([ScholarshipId]) REFERENCES [Scholarships] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RecommendationClickEvents_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_AiRedactionAuditSamples_AiInteractionId] ON [AiRedactionAuditSamples] ([AiInteractionId]);

CREATE INDEX [IX_AiRedactionAuditSamples_ReviewerUserId] ON [AiRedactionAuditSamples] ([ReviewerUserId]);

CREATE INDEX [IX_AiRedactionAuditSamples_SampledAt] ON [AiRedactionAuditSamples] ([SampledAt]);

CREATE INDEX [IX_AiRedactionAuditSamples_UserId] ON [AiRedactionAuditSamples] ([UserId]);

CREATE INDEX [IX_AiRedactionAuditSamples_Verdict_SampledAt] ON [AiRedactionAuditSamples] ([Verdict], [SampledAt]);

CREATE INDEX [IX_RecommendationClickEvents_AiInteractionId] ON [RecommendationClickEvents] ([AiInteractionId]);

CREATE INDEX [IX_RecommendationClickEvents_ScholarshipId_ClickedAt] ON [RecommendationClickEvents] ([ScholarshipId], [ClickedAt]);

CREATE INDEX [IX_RecommendationClickEvents_UserId_ClickedAt] ON [RecommendationClickEvents] ([UserId], [ClickedAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424032238_AddAnalyticsEntities_PB017', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [UserRiskFlags] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Score] decimal(5,4) NOT NULL,
    [IsAtRisk] bit NOT NULL,
    [Reason] nvarchar(500) NULL,
    [ComputedAt] datetimeoffset NOT NULL,
    [SourceRefreshId] uniqueidentifier NULL,
    CONSTRAINT [PK_UserRiskFlags] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRiskFlags_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_UserRiskFlags_IsAtRisk_ComputedAt] ON [UserRiskFlags] ([IsAtRisk], [ComputedAt]);

CREATE UNIQUE INDEX [IX_UserRiskFlags_UserId] ON [UserRiskFlags] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425013910_AddUserRiskFlags_PB018', N'10.0.6');

COMMIT;
GO

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
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260501180102_AddFullTextSearchToScholarships', N'10.0.6');
GO

BEGIN TRANSACTION;
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Applications_StudentId_ScholarshipId'
    AND object_id = OBJECT_ID('Applications')
)
BEGIN
    DROP INDEX [IX_Applications_StudentId_ScholarshipId]
    ON [Applications];
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_Applications_Student_Scholarship_Active'
    AND object_id = OBJECT_ID('Applications')
)
BEGIN
    CREATE UNIQUE INDEX [UX_Applications_Student_Scholarship_Active]
    ON [Applications] ([StudentId], [ScholarshipId])
    WHERE [Status] IN ('Draft', 'Pending', 'UnderReview');
END

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260508003955_RebuildSchema', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Applications]') AND [c].[name] = N'IsReadOnly');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Applications] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Applications] DROP COLUMN [IsReadOnly];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260508074208_AddIsReadOnly_ApplicationTracker', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [PasswordResetTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(128) NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [UsedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NULL,
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);

CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260516174040_AddPasswordResetTokens', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [UserProfiles] ADD [StripeConnectAccountId] nvarchar(256) NULL;

ALTER TABLE [UserProfiles] ADD [StripeConnectOnboardedAt] datetimeoffset NULL;

ALTER TABLE [UserProfiles] ADD [StripeConnectStatus] nvarchar(24) NOT NULL DEFAULT N'None';

ALTER TABLE [Payments] ADD [PayoutId] uniqueidentifier NULL;

CREATE UNIQUE INDEX [UX_ProfitShareConfig_ActivePerType] ON [ProfitShareConfigs] ([PaymentType]) WHERE [EffectiveTo] IS NULL;

CREATE INDEX [IX_Payments_PayoutId] ON [Payments] ([PayoutId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260517003313_AddPayoutInfrastructure_PB013', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_Bookings_ConsultantId_ScheduledStartAt] ON [Bookings];

CREATE UNIQUE INDEX [UX_Bookings_Consultant_Slot_Active] ON [Bookings] ([ConsultantId], [ScheduledStartAt]) WHERE [Status] IN ('Requested', 'Confirmed');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260517015118_AddBookingSlotUniqueIndex_PB006', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [PlatformSettings] (
    [Id] uniqueidentifier NOT NULL,
    [Key] nvarchar(200) NOT NULL,
    [Value] nvarchar(4000) NOT NULL,
    [ValueType] nvarchar(16) NOT NULL,
    [DescriptionEn] nvarchar(1000) NULL,
    [DescriptionAr] nvarchar(1000) NULL,
    [Category] nvarchar(100) NOT NULL,
    [UpdatedByAdminId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_PlatformSettings] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_PlatformSettings_Category] ON [PlatformSettings] ([Category]);

CREATE UNIQUE INDEX [IX_PlatformSettings_Key] ON [PlatformSettings] ([Key]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260517091815_AddPlatformSettings_PB011', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Documents] (
    [Id] uniqueidentifier NOT NULL,
    [OwnerUserId] uniqueidentifier NOT NULL,
    [FileName] nvarchar(260) NOT NULL,
    [ContentType] nvarchar(150) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [StoragePath] nvarchar(1024) NOT NULL,
    [Category] nvarchar(32) NOT NULL,
    [UploadedAt] datetimeoffset NOT NULL,
    [ApplicationTrackerId] uniqueidentifier NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Documents_Applications_ApplicationTrackerId] FOREIGN KEY ([ApplicationTrackerId]) REFERENCES [Applications] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Documents_Users_OwnerUserId] FOREIGN KEY ([OwnerUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Documents_ApplicationTrackerId] ON [Documents] ([ApplicationTrackerId]);

CREATE INDEX [IX_Documents_OwnerUserId_Category] ON [Documents] ([OwnerUserId], [Category]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260517135259_AddDocumentVault_FR216', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserProfiles]') AND [c].[name] = N'Biography');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [UserProfiles] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [UserProfiles] ALTER COLUMN [Biography] nvarchar(max) NULL;

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Applications]') AND [c].[name] = N'PersonalNotes');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Applications] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Applications] ALTER COLUMN [PersonalNotes] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260517150153_AddFieldEncryption_NFR', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [UX_Applications_Student_Scholarship_Active] ON [Applications];

CREATE TABLE [KnowledgeDocuments] (
    [Id] uniqueidentifier NOT NULL,
    [SourceType] nvarchar(24) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [SourceKey] nvarchar(200) NOT NULL,
    [TitleEn] nvarchar(300) NOT NULL,
    [TitleAr] nvarchar(300) NOT NULL,
    [ContentEn] nvarchar(max) NOT NULL,
    [ContentAr] nvarchar(max) NOT NULL,
    [ContentHash] nvarchar(64) NOT NULL,
    [Embedding] varbinary(max) NOT NULL,
    [EmbeddingDimensions] int NOT NULL,
    [EmbeddingModel] nvarchar(64) NULL,
    [IndexedAt] datetimeoffset NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_KnowledgeDocuments] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [UX_Applications_Student_Scholarship_Active] ON [Applications] ([StudentId], [ScholarshipId]) WHERE [Status] <> 'Withdrawn' AND [Status] <> 'Rejected' AND [Status] <> 'Accepted';

CREATE INDEX [IX_KnowledgeDocuments_SourceId] ON [KnowledgeDocuments] ([SourceId]);

CREATE UNIQUE INDEX [IX_KnowledgeDocuments_SourceType_SourceKey] ON [KnowledgeDocuments] ([SourceType], [SourceKey]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260518001028_AddKnowledgeBase_RAG', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [UserProfiles] ADD [BiographyAr] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260518184411_AddConsultantBiographyAr', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [FinancialConfigRules] (
    [Id] uniqueidentifier NOT NULL,
    [PaymentType] nvarchar(32) NOT NULL,
    [FeeKind] nvarchar(16) NOT NULL,
    [FeePercentage] decimal(5,4) NULL,
    [FeeAmountCents] bigint NULL,
    [ProfitSharePercentage] decimal(5,4) NOT NULL,
    [Status] nvarchar(16) NOT NULL,
    [EffectiveFrom] datetimeoffset NOT NULL,
    [EffectiveTo] datetimeoffset NULL,
    [SetByAdminId] uniqueidentifier NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_FinancialConfigRules] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_FinancialConfigRules_PaymentType_Status] ON [FinancialConfigRules] ([PaymentType], [Status]);

CREATE UNIQUE INDEX [UX_FinancialConfigRule_ActivePerType] ON [FinancialConfigRules] ([PaymentType]) WHERE [Status] = 'Active';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260519012326_AddFinancialConfigRules', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [UserProfiles] ADD [BookingIntakeSuspendedAt] datetimeoffset NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260519025832_AddConsultantBookingIntakeSuspension', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;

CREATE OR ALTER VIEW dbo.vw_funnel_daily AS
WITH registrations AS (
    SELECT CAST(CreatedAt AS date) AS ActivityDate,
           COUNT(*) AS Registrations,
           SUM(CASE WHEN IsOnboardingComplete = 1 THEN 1 ELSE 0 END) AS OnboardingCompleted
    FROM dbo.Users
    WHERE IsDeleted = 0
    GROUP BY CAST(CreatedAt AS date)
),
submitted AS (
    SELECT CAST(SubmittedAt AS date) AS ActivityDate,
           COUNT(*) AS ApplicationsSubmitted
    FROM dbo.Applications
    WHERE IsDeleted = 0 AND SubmittedAt IS NOT NULL
    GROUP BY CAST(SubmittedAt AS date)
),
accepted AS (
    SELECT CAST(COALESCE(DecisionAt, UpdatedAt, CreatedAt) AS date) AS ActivityDate,
           COUNT(*) AS ApplicationsAccepted
    FROM dbo.Applications
    WHERE IsDeleted = 0 AND Status = 'Accepted'
    GROUP BY CAST(COALESCE(DecisionAt, UpdatedAt, CreatedAt) AS date)
),
spine AS (
    SELECT ActivityDate FROM registrations
    UNION SELECT ActivityDate FROM submitted
    UNION SELECT ActivityDate FROM accepted
)
SELECT s.ActivityDate,
       COALESCE(r.Registrations, 0)           AS Registrations,
       COALESCE(r.OnboardingCompleted, 0)     AS OnboardingCompleted,
       COALESCE(sub.ApplicationsSubmitted, 0) AS ApplicationsSubmitted,
       COALESCE(acc.ApplicationsAccepted, 0)  AS ApplicationsAccepted
FROM spine s
LEFT JOIN registrations r ON r.ActivityDate = s.ActivityDate
LEFT JOIN submitted sub   ON sub.ActivityDate = s.ActivityDate
LEFT JOIN accepted acc    ON acc.ActivityDate = s.ActivityDate;


CREATE OR ALTER VIEW dbo.vw_acceptance_rates AS
SELECT
    s.Id                                      AS ScholarshipId,
    s.TitleEn                                 AS ScholarshipTitleEn,
    s.TitleAr                                 AS ScholarshipTitleAr,
    COALESCE(c.NameEn, 'Uncategorized')       AS FieldEn,
    COALESCE(c.NameAr, 'Uncategorized')       AS FieldAr,
    COALESCE(u.CountryOfResidence, 'Unknown') AS StudentCountry,
    COUNT(*)                                                       AS TotalApplications,
    SUM(CASE WHEN a.SubmittedAt IS NOT NULL THEN 1 ELSE 0 END)      AS SubmittedApplications,
    SUM(CASE WHEN a.Status = 'Accepted' THEN 1 ELSE 0 END)         AS AcceptedApplications,
    SUM(CASE WHEN a.Status = 'Rejected' THEN 1 ELSE 0 END)         AS RejectedApplications,
    CAST(100.0 * SUM(CASE WHEN a.Status = 'Accepted' THEN 1 ELSE 0 END)
         / NULLIF(SUM(CASE WHEN a.Status IN ('Accepted', 'Rejected') THEN 1 ELSE 0 END), 0)
         AS decimal(5, 2))                     AS AcceptanceRatePercent
FROM dbo.Applications a
JOIN dbo.Scholarships s   ON s.Id = a.ScholarshipId
LEFT JOIN dbo.Categories c ON c.Id = s.CategoryId
LEFT JOIN dbo.Users u      ON u.Id = a.StudentId
WHERE a.IsDeleted = 0
GROUP BY s.Id, s.TitleEn, s.TitleAr, c.NameEn, c.NameAr, u.CountryOfResidence;


CREATE OR ALTER VIEW dbo.vw_finance_daily AS
WITH booking_payments AS (
    SELECT CAST(CapturedAt AS date)            AS ActivityDate,
           COUNT(*)                            AS CapturedCount,
           SUM(AmountCents) / 100.0            AS GrossUsd,
           SUM(ProfitShareAmountCents) / 100.0 AS ProfitShareUsd,
           SUM(PayeeAmountCents) / 100.0       AS PayeeNetUsd,
           SUM(RefundedAmountCents) / 100.0    AS RefundedUsd,
           SUM(CASE WHEN RefundedAmountCents > 0 THEN 1 ELSE 0 END) AS RefundCount
    FROM dbo.Payments
    WHERE IsDeleted = 0 AND CapturedAt IS NOT NULL
    GROUP BY CAST(CapturedAt AS date)
),
review_payments AS (
    SELECT CAST(CapturedAt AS date)            AS ActivityDate,
           COUNT(*)                            AS CapturedCount,
           SUM(AmountUsd)                      AS GrossUsd,
           SUM(ProfitShareAmountUsd)           AS ProfitShareUsd,
           SUM(PayeeAmountUsd)                 AS PayeeNetUsd,
           SUM(COALESCE(RefundedAmountUsd, 0)) AS RefundedUsd,
           SUM(CASE WHEN COALESCE(RefundedAmountUsd, 0) > 0 THEN 1 ELSE 0 END) AS RefundCount
    FROM dbo.CompanyReviewPayments
    WHERE CapturedAt IS NOT NULL
    GROUP BY CAST(CapturedAt AS date)
)
SELECT ActivityDate,
       CAST('ConsultantBooking' AS nvarchar(20)) AS RevenueStream,
       CapturedCount,
       CAST(GrossUsd AS decimal(14, 2))       AS GrossUsd,
       CAST(ProfitShareUsd AS decimal(14, 2)) AS ProfitShareUsd,
       CAST(PayeeNetUsd AS decimal(14, 2))    AS PayeeNetUsd,
       CAST(RefundedUsd AS decimal(14, 2))    AS RefundedUsd,
       RefundCount
FROM booking_payments
UNION ALL
SELECT ActivityDate,
       CAST('CompanyReview' AS nvarchar(20)) AS RevenueStream,
       CapturedCount,
       CAST(GrossUsd AS decimal(14, 2)),
       CAST(ProfitShareUsd AS decimal(14, 2)),
       CAST(PayeeNetUsd AS decimal(14, 2)),
       CAST(RefundedUsd AS decimal(14, 2)),
       RefundCount
FROM review_payments;


CREATE OR ALTER VIEW dbo.vw_consultant_kpis AS
SELECT
    u.Id                           AS ConsultantId,
    u.FirstName + ' ' + u.LastName AS ConsultantName,
    u.Email                        AS ConsultantEmail,
    COALESCE(bk.TotalBookings, 0)     AS TotalBookings,
    COALESCE(bk.CompletedBookings, 0) AS CompletedBookings,
    COALESCE(bk.CancelledBookings, 0) AS CancelledBookings,
    COALESCE(bk.RejectedBookings, 0)  AS RejectedBookings,
    COALESCE(bk.ConsultantNoShows, 0) AS ConsultantNoShows,
    COALESCE(bk.StudentNoShows, 0)    AS StudentNoShows,
    CAST(COALESCE(bk.CompletedRevenueUsd, 0) AS decimal(12, 2)) AS CompletedRevenueUsd,
    COALESCE(rv.ReviewCount, 0)       AS ReviewCount,
    rv.AverageRating
FROM dbo.Users u
JOIN dbo.UserRoles ur ON ur.UserId = u.Id
JOIN dbo.Roles r      ON r.Id = ur.RoleId AND r.Name = 'Consultant'
LEFT JOIN (
    SELECT ConsultantId,
           COUNT(*)                                                AS TotalBookings,
           SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END)   AS CompletedBookings,
           SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END)   AS CancelledBookings,
           SUM(CASE WHEN Status = 'Rejected'  THEN 1 ELSE 0 END)   AS RejectedBookings,
           SUM(CASE WHEN IsNoShowConsultant = 1 THEN 1 ELSE 0 END) AS ConsultantNoShows,
           SUM(CASE WHEN IsNoShowStudent = 1 THEN 1 ELSE 0 END)    AS StudentNoShows,
           SUM(CASE WHEN Status = 'Completed' THEN PriceUsd ELSE 0 END) AS CompletedRevenueUsd
    FROM dbo.Bookings
    WHERE IsDeleted = 0
    GROUP BY ConsultantId
) bk ON bk.ConsultantId = u.Id
LEFT JOIN (
    SELECT ConsultantId,
           COUNT(*) AS ReviewCount,
           CAST(AVG(CAST(Rating AS decimal(4, 2))) AS decimal(4, 2)) AS AverageRating
    FROM dbo.ConsultantReviews
    WHERE IsDeleted = 0 AND IsHiddenByAdmin = 0
    GROUP BY ConsultantId
) rv ON rv.ConsultantId = u.Id
WHERE u.IsDeleted = 0;


CREATE OR ALTER VIEW dbo.vw_student_journey AS
SELECT
    u.Id                           AS StudentId,
    u.FirstName + ' ' + u.LastName AS StudentName,
    u.Email                        AS StudentEmail,
    u.CountryOfResidence           AS StudentCountry,
    u.CreatedAt                    AS RegisteredAt,
    u.IsOnboardingComplete         AS OnboardingComplete,
    u.LastLoginAt                  AS LastLoginAt,
    COALESCE(ap.TotalApplications, 0)     AS TotalApplications,
    COALESCE(ap.SubmittedApplications, 0) AS SubmittedApplications,
    COALESCE(ap.AcceptedApplications, 0)  AS AcceptedApplications,
    ap.LastApplicationAt,
    COALESCE(bk.TotalBookings, 0)     AS TotalBookings,
    COALESCE(bk.CompletedBookings, 0) AS CompletedBookings,
    bk.LastBookingAt
FROM dbo.Users u
JOIN dbo.UserRoles ur ON ur.UserId = u.Id
JOIN dbo.Roles r      ON r.Id = ur.RoleId AND r.Name = 'Student'
LEFT JOIN (
    SELECT StudentId,
           COUNT(*)                                               AS TotalApplications,
           SUM(CASE WHEN SubmittedAt IS NOT NULL THEN 1 ELSE 0 END) AS SubmittedApplications,
           SUM(CASE WHEN Status = 'Accepted' THEN 1 ELSE 0 END)    AS AcceptedApplications,
           MAX(CreatedAt)                                          AS LastApplicationAt
    FROM dbo.Applications
    WHERE IsDeleted = 0
    GROUP BY StudentId
) ap ON ap.StudentId = u.Id
LEFT JOIN (
    SELECT StudentId,
           COUNT(*)                                              AS TotalBookings,
           SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedBookings,
           MAX(CreatedAt)                                        AS LastBookingAt
    FROM dbo.Bookings
    WHERE IsDeleted = 0
    GROUP BY StudentId
) bk ON bk.StudentId = u.Id
WHERE u.IsDeleted = 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260519053514_AddReportingViews_PB015', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Bookings] ADD [ConsultantJoinedAt] datetimeoffset NULL;

ALTER TABLE [Bookings] ADD [StudentJoinedAt] datetimeoffset NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260519112756_AddBookingMeetingJoinTracking', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Bookings] ADD [MeetingRoomId] nvarchar(64) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260519115605_AddBookingMeetingRoomId', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Bookings] ADD [RecordingId] nvarchar(256) NULL;

ALTER TABLE [Bookings] ADD [RecordingStartedAt] datetimeoffset NULL;

CREATE TABLE [SessionRecordings] (
    [Id] uniqueidentifier NOT NULL,
    [BookingId] uniqueidentifier NOT NULL,
    [RecordingId] nvarchar(256) NOT NULL,
    [StoragePath] nvarchar(1024) NOT NULL,
    [ContentType] nvarchar(150) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [RecordedAt] datetimeoffset NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_SessionRecordings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SessionRecordings_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_SessionRecordings_BookingId] ON [SessionRecordings] ([BookingId]);

CREATE INDEX [IX_SessionRecordings_RecordingId] ON [SessionRecordings] ([RecordingId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260519164659_AddSessionRecording_PB006', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'MeetingUrl');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Bookings] DROP COLUMN [MeetingUrl];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260519174304_RemoveBookingMeetingUrl', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Bookings] ADD [StudentNotes] nvarchar(2000) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260519234508_AddBookingStudentNotes_PB006', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Scholarships] ADD [FieldsOfStudyJson] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520035127_AddScholarshipFieldsOfStudy_PB003', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationEmail' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationEmail] nvarchar(256) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationCountry' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationCountry] nvarchar(80) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationTaxNumber' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationTaxNumber] nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompanyType' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [CompanyType] nvarchar(40) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompanyDescription' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [CompanyDescription] nvarchar(1000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPersonFullName' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPersonFullName] nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPersonPosition' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPersonPosition] nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPhoneNumber' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPhoneNumber] nvarchar(40) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ProfessionalTitle' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ProfessionalTitle] nvarchar(150) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'HighestDegree' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [HighestDegree] nvarchar(150) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'FieldOfExpertise' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [FieldOfExpertise] nvarchar(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'YearsOfExperience' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [YearsOfExperience] int NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'PortfolioUrl' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [PortfolioUrl] nvarchar(2048) NULL;



IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ExternalTitle' AND Object_ID = Object_ID(N'Applications'))
    ALTER TABLE [Applications] ADD [ExternalTitle] nvarchar(500) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ExternalProvider' AND Object_ID = Object_ID(N'Applications'))
    ALTER TABLE [Applications] ADD [ExternalProvider] nvarchar(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'Deadline' AND Object_ID = Object_ID(N'Applications'))
    ALTER TABLE [Applications] ADD [Deadline] datetimeoffset NULL;



IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Applications_Student_Scholarship_Active' AND object_id = OBJECT_ID(N'Applications'))
    DROP INDEX [UX_Applications_Student_Scholarship_Active] ON [Applications];

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'ScholarshipId' AND Object_ID = Object_ID(N'Applications') AND is_nullable = 0
)
BEGIN
    DECLARE @fk_name SYSNAME;
    SELECT @fk_name = name FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'Applications')
          AND OBJECT_NAME(referenced_object_id) = N'Scholarships';
    IF @fk_name IS NOT NULL EXEC('ALTER TABLE [Applications] DROP CONSTRAINT [' + @fk_name + ']');

    ALTER TABLE [Applications] ALTER COLUMN [ScholarshipId] uniqueidentifier NULL;

    ALTER TABLE [Applications] ADD CONSTRAINT [FK_Applications_Scholarships_ScholarshipId]
        FOREIGN KEY ([ScholarshipId]) REFERENCES [Scholarships]([Id]) ON DELETE NO ACTION;
END


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521163702_ExpandUserProfileAndApplications', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IsTaxRegistered' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [IsTaxRegistered] bit NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TaxNotApplicableReason' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [TaxNotApplicableReason] nvarchar(500) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IsLegallyRegistered' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [IsLegallyRegistered] bit NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LegalRegistrationNotApplicableReason' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [LegalRegistrationNotApplicableReason] nvarchar(500) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LastOnboardingRejectionReason' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [LastOnboardingRejectionReason] nvarchar(2000) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'LastOnboardingRejectedAt' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [LastOnboardingRejectedAt] datetimeoffset NULL;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521211050_AddOnboardingApplicabilityFields_AuthCode03', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
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

CREATE INDEX [IX_CompanyReviewRequests_CompanyId_Status] ON [CompanyReviewRequests] ([CompanyId], [Status]);

CREATE INDEX [IX_CompanyReviewRequests_PaymentId] ON [CompanyReviewRequests] ([PaymentId]);

CREATE INDEX [IX_CompanyReviewRequests_ScholarshipId] ON [CompanyReviewRequests] ([ScholarshipId]);

CREATE INDEX [IX_CompanyReviewRequests_StudentId_Status] ON [CompanyReviewRequests] ([StudentId], [Status]);

CREATE UNIQUE INDEX [UX_CompanyReviewRequests_Student_Scholarship_Active] ON [CompanyReviewRequests] ([StudentId], [ScholarshipId]) WHERE [Status] IN ('Draft', 'Submitted', 'Pending', 'UnderReview');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260522033457_AddCompanyReviewRequests_PB005', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [UserProfiles] ADD [CompanyAverageRating] decimal(3,2) NULL;

ALTER TABLE [UserProfiles] ADD [CompanyLowRatingFlaggedAt] datetimeoffset NULL;

ALTER TABLE [UserProfiles] ADD [CompanyReviewCount] int NOT NULL DEFAULT 0;

CREATE INDEX [IX_UserProfiles_CompanyLowRatingFlagged] ON [UserProfiles] ([CompanyLowRatingFlaggedAt]) WHERE [CompanyLowRatingFlaggedAt] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260522153032_AddCompanyLowRatingFields_PB005R', N'10.0.6');

COMMIT;
GO

BEGIN TRANSACTION;

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

COMMIT;
GO

