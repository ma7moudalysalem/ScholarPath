# ScholarPath — Relational Mapping (ER/EER → Relations)

> Derived by applying **Elmasri & Navathe's** ER/EER-to-relational mapping
> algorithm to the EERD in `01-EERD.md`, then reconciled against the live
> EF Core model snapshot (`ApplicationDbContextModelSnapshot.cs`) so the
> result matches the **actual deployed schema** (Azure SQL).

## Notation

- <u>Underlined</u> = **primary key**. All PKs are `Guid` (`uniqueidentifier`)
  unless noted; junction tables use a **composite** PK.
- *Italic* = **foreign key**. Each FK is detailed under its relation with its
  delete rule: **Cascade**, **Restrict**, or **SetNull**.
- A `Guid` attribute marked **(soft)** is a *loose reference* — it points at
  another relation logically but has **no `FOREIGN KEY` constraint** (integrity
  enforced by application code). This is a deliberate decoupling for
  high-volume / audit / analytics relations.
- `★UK` = unique index/constraint, `▽FUK` = *filtered* unique index (partial),
  `enc` = encrypted column, `¢` = money stored as integer cents.
- Audit columns `CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId,
  RowVersion` (from `AuditableEntity`) and soft-delete columns
  `IsDeleted, DeletedAt, DeletedByUserId` are abbreviated **{audit}** and
  **{softdel}** to keep the schemas readable.

---

## How the EER constructs were mapped (Elmasri's algorithm)

| Step | Construct | Rule applied | Where it appears here |
|---|---|---|---|
| 1 | **Regular (strong) entity** | One relation; pick PK | `USERS`, `SCHOLARSHIPS`, `BOOKINGS`, … (every strong entity) |
| 2 | **Weak entity** | Relation incl. owner's PK as part of a FK; identity = owner PK + partial key | child/EAV relations: `SCHOLARSHIP_CHILDREN`, `APPLICATION_CHILDREN`, `EDUCATION_ENTRIES`, `RESOURCE_CHAPTERS`, `*_FILES/_LINKS` |
| 3 | **1:1 relationship** | FK (+ `★UK`) on the side with total participation | `USERS`–`USER_PROFILES`; `BOOKINGS`–`CONSULTANT_REVIEWS`; `APPLICATIONS`–`COMPANY_REVIEWS`; `AI_INTERACTIONS`–`AI_REDACTION_AUDIT_SAMPLES`; `USERS`–`USER_RISK_FLAGS` |
| 4 | **1:N relationship** | FK on the N-side referencing the 1-side | `SCHOLARSHIPS`→`APPLICATIONS`, `USERS`→`BOOKINGS`, `FORUM_POSTS`→`FORUM_VOTES`, … |
| 5 | **M:N relationship** | New **relationship relation** with composite PK = the two FKs | `USER_ROLES` (User⋈Role), `FORUM_POST_TAGS` (Post⋈Tag); `SAVED_SCHOLARSHIPS` is the same pattern but **uniqueness only** (loose) |
| 6 | **Multivalued attribute** | Separate relation keyed by (owner PK + value) | realized as JSON columns (`PreferredCountriesJson`, `TagsJson`, …) **or** child rows (`SCHOLARSHIP_CHILDREN`) — see note ‡ |
| 7 | **N-ary relationship** | New relation with a FK to each participant | `COMPANY_REVIEW_REQUESTS` (Student × Company × Scholarship × Payment) |
| 8 | **Specialization / generalization (EER)** | *Option C — single relation with a type discriminator* | the USER **ISA** {Student, Company, Consultant, Admin}: one `USERS` table + role rows in `USER_ROLES`; role-specific attributes folded into one `USER_PROFILES` relation (NULL when N/A) |
| 9 | **Union / category** | — | not used (no category types in the model) |

> ‡ **Deviation from textbook step 6:** multivalued attributes (a student's
> preferred countries, a post's tags, a payout's payment ids) are stored as
> **JSON string columns** rather than separate relations, except where they
> needed querying/ordering (community tags became the real `FORUM_TAGS` +
> `FORUM_POST_TAGS` M:N). This denormalization is recorded honestly so the
> mapping matches the deployed schema.

---

## 1. Identity, Access & Profile

```
USERS( Id, Email, NormalizedEmail, FirstName, LastName, PasswordHash,
       SecurityStamp, ConcurrencyStamp, EmailConfirmed, PhoneNumber,
       LockoutEnd, LockoutEnabled, AccessFailedCount, TwoFactorEnabled,
       ProfileImageUrl, AccountStatus, ActiveRole, IsOnboardingComplete,
       PreferredLanguage, CountryOfResidence, LastLoginAt, {audit}, {softdel} )
```
*(PK Id underlined; ASP.NET Identity principal — table renamed `Users`. Identity
creates a **unique** filtered index on `NormalizedUserName` (`UserNameIndex`) but a
**non-unique** index on `NormalizedEmail` (`EmailIndex`); e-mail uniqueness is
enforced by the Identity validator, not a DB unique constraint — so no `★UK` here.)*

```
ROLES( Id, Name, NormalizedName★UK, ConcurrencyStamp, Description, CreatedAt )

USER_ROLES( UserId, RoleId )                       -- M:N junction (step 5)
    FK UserId → USERS(Id)  [Restrict]   -- NO ACTION: no FK cascades from Users
    FK RoleId → ROLES(Id)  [Cascade]
    PK = (UserId, RoleId)

USER_CLAIMS / USER_LOGINS / USER_TOKENS / ROLE_CLAIMS -- standard Identity relations
```

```
USER_PROFILES( Id, UserId★UK, Biography enc, BiographyAr, DateOfBirth,
       Nationality, LinkedInUrl, WebsiteUrl, Timezone,
       -- student:  AcademicLevel, FieldOfStudy, CurrentInstitution, Gpa(4,2),
       --           GpaScale, PreferredCountriesJson, PreferredFieldsJson,
       -- company:  OrganizationLegalName, OrganizationRegistrationNumber,
       --           OrganizationVerificationStatus, IsTaxRegistered, …,
       --           CompanyAverageRating(3,2), CompanyReviewCount,
       --           CompanyLowRatingFlaggedAt,
       -- consultant: SessionFeeUsd(10,2), SessionDurationMinutes,
       --           ExpertiseTagsJson, LanguagesJson, ProfessionalTitle,
       --           YearsOfExperience, BookingIntakeSuspendedAt,
       --           StripeConnectAccountId, StripeConnectStatus,
       ProfileCompletenessPercent, {audit} )
    FK UserId → USERS(Id)  [Restrict]     -- 1:1 (★UK enforces it)  (step 3)
```
> All three role-attribute groups live in this **one** relation (EER step 8,
> single-table). The **filtered** (non-unique) index
> `IX_UserProfiles_CompanyLowRatingFlagged` on
> `CompanyLowRatingFlaggedAt WHERE … IS NOT NULL` drives the low-rated queue.

```
EDUCATION_ENTRIES( Id, UserProfileId, InstitutionName, Degree, FieldOfStudy,
       StartDate, EndDate, Gpa(4,2), Description, {audit} )      -- weak (step 2)
    FK UserProfileId → USER_PROFILES(Id)  [Cascade]

EXPERTISE_TAGS( Id, Slug★UK, NameEn, NameAr, Category )          -- lookup, no FK

REFRESH_TOKENS( Id, UserId, TokenHash★UK, ExpiresAt, IsRevoked, RevokedAt,
       RevokedReason, ReplacedByTokenId(soft), IpAddress, UserAgent, {audit} )
    FK UserId → USERS(Id)  [Restrict]
    CI (UserId, IsRevoked)

LOGIN_ATTEMPTS( Id, Email, UserId(soft), Succeeded, FailureReason, OccurredAt,
       IpAddress, UserAgent )                       CI (Email, OccurredAt)

PASSWORD_RESET_TOKENS( Id, UserId, TokenHash★UK, ExpiresAt, UsedAt, {audit} )
    FK UserId → USERS(Id)  [Restrict]

UPGRADE_REQUESTS( Id, UserId, Target, Status, Reason, ReviewerNotes,
       ReviewedByAdminId(soft), ReviewedAt, {audit}, {softdel} )
    FK UserId → USERS(Id)  [Restrict]   -- real FK; ReviewedByAdminId is loose
    CI (UserId, Status)
    -- SCOPE: backs the Student→Consultant role upgrade ONLY. First-time
    -- Company/Consultant onboarding is NOT stored here — it lives on
    -- USER_PROFILES + USERS.AccountStatus(PendingApproval), with verification
    -- files as DOCUMENTS(Category=OnboardingDocument). Target='Company' and the
    -- _FILES/_LINKS children are seed/vestigial (no live handler writes them).

UPGRADE_REQUEST_FILES( Id, UpgradeRequestId, FileName, BlobUrl, SizeBytes,
       ContentType, UploadedAt )                                 -- weak (step 2)
    FK UpgradeRequestId → UPGRADE_REQUESTS(Id)  [Cascade]

UPGRADE_REQUEST_LINKS( Id, UpgradeRequestId, Label, Url )         -- weak (step 2)
    FK UpgradeRequestId → UPGRADE_REQUESTS(Id)  [Cascade]
```

---

## 2. Scholarships, Applications & Documents

```
CATEGORIES( Id, Slug★UK, NameEn, NameAr, DescriptionEn, DescriptionAr,
       IconKey, DisplayOrder, {audit} )

SCHOLARSHIPS( Id, CategoryId, OwnerCompanyId, CreatedByAdminId(soft),
       Slug★UK, TitleEn, TitleAr, DescriptionEn, DescriptionAr, Mode,
       ExternalApplicationUrl, Status, Deadline, OpenedAt, ArchivedAt,
       IsFeatured, FeaturedOrder, FundingType, FundingAmountUsd(14,2),
       Currency, TargetLevel, TargetCountriesJson, FieldsOfStudyJson,
       EligibilityRequirementsEn/Ar, TagsJson, ApplicationFormSchemaJson,
       RequiredDocumentsJson, ReviewFeeUsd(10,2), {audit}, {softdel} )
    FK CategoryId     → CATEGORIES(Id)  [SetNull]
    FK OwnerCompanyId → USERS(Id)       [SetNull]    -- NULL = admin/external
    CI (Status, Deadline)

SCHOLARSHIP_CHILDREN( Id, ScholarshipId, ChildType, KeyEn, KeyAr,
       ValueEn, ValueAr, SortOrder )                 CI (ScholarshipId, ChildType)
    FK ScholarshipId → SCHOLARSHIPS(Id)  [Cascade]   -- weak EAV entity (step 2)

SAVED_SCHOLARSHIPS( Id, UserId(soft), ScholarshipId, SavedAt, Note )
    FK ScholarshipId → SCHOLARSHIPS(Id)  [Cascade]   -- ScholarshipId is a real FK
    ★UK (UserId, ScholarshipId)        -- M:N bookmark; UserId is loose, no FK

APPLICATIONS( Id, StudentId, ScholarshipId, Mode, Status, FormDataJson,
       AttachedDocumentsJson, ExternalTrackingUrl, ExternalReferenceId,
       ExternalTitle, ExternalProvider, Deadline, SubmittedAt, WithdrawnAt,
       ReviewStartedAt, DecisionAt, DecisionReason, NextReminderAt,
       PersonalNotes enc, {audit}, {softdel} )
    FK StudentId     → USERS(Id)         [Restrict]
    FK ScholarshipId → SCHOLARSHIPS(Id)  [Restrict]   -- NULL = external tracker
    CI IX_Applications_ScholarshipId (ScholarshipId), IX_Applications_Status (Status)
    -- ⚠ NO DB filtered-unique index on Applications in production (verified
    -- against the live dump). The single-active rule (FR-057) is enforced in
    -- APPLICATION CODE only — the old UX_Applications_Student_Scholarship_Active
    -- was dropped when ScholarshipId became nullable (migration 20260521163702)
    -- and never recreated. (Contrast CompanyReviewRequests below, which keeps its
    -- filtered-unique — that one IS DB-enforced.)

APPLICATION_CHILDREN( Id, ApplicationTrackerId, ChildType, Title,
       Content, MetadataJson, OccurredAt, ActorUserId(soft), SortOrder )
       CI (ApplicationTrackerId, ChildType)           -- status-history / notes
    FK ApplicationTrackerId → APPLICATIONS(Id)  [Cascade]   -- weak entity (step 2);
                                                            -- ActorUserId stays loose

DOCUMENTS( Id, OwnerUserId, ApplicationTrackerId, FileName, ContentType,
       SizeBytes, StoragePath, Category, UploadedAt, {audit}, {softdel} )
    FK OwnerUserId          → USERS(Id)         [Restrict]
    FK ApplicationTrackerId → APPLICATIONS(Id)   [SetNull]   -- optional link
    CI (OwnerUserId, Category)

COMPANY_REVIEW_REQUESTS( Id, StudentId, CompanyId, ScholarshipId,
       ApplicationTrackerId(soft), PaymentId, Status, ReviewFeeUsdSnapshot(10,2),
       Currency, SubmittedAt, AcceptedAt, RejectedAt, CompletedAt, ClosedAt,
       CancelledAt, ExpiredAt, PendingExpiresAt, CancelReason, RejectReason,
       {audit}, {softdel} )                                       -- N-ary (step 7)
    FK StudentId     → USERS(Id)         [Restrict]
    FK CompanyId     → USERS(Id)         [Restrict]
    FK ScholarshipId → SCHOLARSHIPS(Id)  [Restrict]
    FK PaymentId     → PAYMENTS(Id)      [SetNull]
    ▽FUK (StudentId, ScholarshipId) WHERE Status IN ('Draft','Submitted','Pending','UnderReview')

COMPANY_REVIEWS( Id, ApplicationTrackerId★UK, StudentId, CompanyId, Rating,
       Comment, IsHiddenByAdmin, AdminNote, {audit}, {softdel} )  -- 1:1 (step 3)
    FK ApplicationTrackerId → APPLICATIONS(Id)  [Restrict]   ★UK ⇒ one per application
    FK StudentId            → USERS(Id)          [Restrict]
    FK CompanyId            → USERS(Id)          [Restrict]

COMPANY_REVIEW_PAYMENTS( Id, …amounts(14,2)…, StripePaymentIntentId★UK,
       IdempotencyKey★UK, {audit} )    -- LEGACY / DEAD (verified): no production
    -- handler writes it (seed-only); the live paid-review flow is
    -- COMPANY_REVIEW_REQUESTS.PaymentId → PAYMENTS(Type=CompanyReview).
    -- Kept in schema; analytics still sum it (reads 0 from the empty table).
```

---

## 3. Consultant Booking, Payments, Ratings & Recording

```
AVAILABILITIES( Id, ConsultantId, DayOfWeek, StartTime, EndTime,
       SpecificStartAt, SpecificEndAt, Timezone, IsRecurring, IsActive,
       {audit}, {softdel} )                          CI (ConsultantId, IsActive)
    FK ConsultantId → USERS(Id)  [Restrict]
    -- recurring (DayOfWeek+Start+End) XOR ad-hoc (SpecificStart+SpecificEnd)

BOOKINGS( Id, StudentId, ConsultantId, AvailabilityId, PaymentId,
       ScheduledStartAt, ScheduledEndAt, DurationMinutes, PriceUsd(10,2),
       StudentNotes, MeetingRoomId, RecordingStartedAt, RecordingId, Status,
       RequestedAt, ConfirmedAt, RejectedAt, ExpiredAt, CancelledAt,
       CompletedAt, CancellationReason, CancelledByUserId(soft),
       StripePaymentIntentId, IsNoShowStudent, IsNoShowConsultant,
       NoShowMarkedAt, StudentJoinedAt, ConsultantJoinedAt, {audit}, {softdel} )
    FK StudentId      → USERS(Id)           [Restrict]
    FK ConsultantId   → USERS(Id)           [Restrict]
    FK AvailabilityId → AVAILABILITIES(Id)  [SetNull]
    FK PaymentId      → PAYMENTS(Id)         [SetNull]
    ▽FUK (ConsultantId, ScheduledStartAt) WHERE Status IN ('Requested','Confirmed')
    CI (StudentId, Status)

PAYMENTS( Id, PayerUserId(soft), PayeeUserId(soft), RelatedBookingId(soft),
       RelatedApplicationId(soft), PayoutId(soft), Type, Status,
       AmountCents¢, Currency, ProfitShareAmountCents¢, PayeeAmountCents¢,
       RefundedAmountCents¢, StripePaymentIntentId, StripeChargeId,
       IdempotencyKey★UK, HeldAt, CapturedAt, RefundedAt, RefundReason,
       FailureReason, {audit}, {softdel} )
    -- NO database FKs by design; CI (PayerUserId, Status), (PayeeUserId, Status)

PAYOUTS( Id, PayeeUserId(soft), AmountCents¢, Currency, Status, StripePayoutId,
       StripeConnectAccountId, InitiatedAt, PaidAt, FailureReason,
       IncludedPaymentIdsJson, {audit} )            CI (PayeeUserId, Status)
    -- aggregates N PAYMENTS via JSON id-list + PAYMENTS.PayoutId (both loose)

CONSULTANT_REVIEWS( Id, BookingId★UK, StudentId, ConsultantId, Rating,
       Comment, IsHiddenByAdmin, AdminNote, {audit}, {softdel} )  -- 1:1 (step 3)
    FK BookingId     → BOOKINGS(Id)  [Restrict]   ★UK ⇒ one review per booking
    FK StudentId     → USERS(Id)     [Restrict]
    FK ConsultantId  → USERS(Id)     [Restrict]
    CI (ConsultantId, IsHiddenByAdmin, IsDeleted)

SESSION_RECORDINGS( Id, BookingId, RecordingId, StoragePath, ContentType,
       SizeBytes, RecordedAt, {audit}, {softdel} )
    FK BookingId → BOOKINGS(Id)  [Restrict]        -- (non-unique: 1:N at DB level)

STRIPE_WEBHOOK_EVENTS( Id, StripeEventId★UK, …, IsProcessed, ReceivedAt )  -- idempotency log
PROFIT_SHARE_CONFIGS( Id, PaymentType, Percentage(5,4), EffectiveFrom,
       EffectiveTo, SetByAdminId(soft), {audit} )   -- ▽FUK one active per PaymentType
FINANCIAL_CONFIG_RULES( Id, PaymentType, FeeKind, …(5,4)…, Status,
       SetByAdminId(soft), {audit} )                -- ▽FUK one active per PaymentType
```
> All FKs touching `USERS` use **Restrict** to avoid SQL Server's *multiple
> cascade paths* (error 1785) — a row may reference `Users` two or three times
> (student + consultant, payer + payee, subject + reviewer). The nullable
> booking FKs (`AvailabilityId`, `PaymentId`) use **SetNull**.

---

## 4. Community & Chat

```
FORUM_CATEGORIES( Id, Slug★UK, NameEn, NameAr, DescriptionEn, DescriptionAr,
       DisplayOrder, IsActive, {audit} )

FORUM_POSTS( Id, AuthorId, CategoryId, ParentPostId, Title, BodyMarkdown,
       ModerationStatus, UpvoteCount, DownvoteCount, FlagCount, ReplyCount,
       IsAutoHidden, AutoHiddenAt, ModeratedByAdminId(soft), ModeratedAt,
       ModerationNote, {audit}, {softdel} )
    FK AuthorId     → USERS(Id)            [Restrict]
    FK CategoryId   → FORUM_CATEGORIES(Id) [SetNull]
    FK ParentPostId → FORUM_POSTS(Id)      [Restrict]   -- self-ref: NULL = root thread
    CI (CategoryId, CreatedAt)

FORUM_POST_ATTACHMENTS( Id, ForumPostId, FileName, BlobUrl, ContentType, SizeBytes )
    FK ForumPostId → FORUM_POSTS(Id)  [Cascade]
    -- PLANNED / schema-only (verified): no upload or read path exists today;
    -- forum posts are markdown-only. Table + nav present, nothing populates it.

FORUM_VOTES( Id, ForumPostId, UserId(soft), VoteType, VotedAt )
    FK ForumPostId → FORUM_POSTS(Id)  [Cascade]    ★UK (ForumPostId, UserId)

FORUM_FLAGS( Id, ForumPostId, FlaggedByUserId(soft), Reason, AdditionalDetails,
       FlaggedAt, IsValid )
    FK ForumPostId → FORUM_POSTS(Id)  [Cascade]    ★UK (ForumPostId, FlaggedByUserId)

FORUM_BOOKMARKS( Id, ForumPostId, UserId(soft), CreatedAt )
    FK ForumPostId → FORUM_POSTS(Id)  [Cascade]    ★UK (ForumPostId, UserId)

FORUM_TAGS( Id, Name, Slug★UK, CreatedAt )

FORUM_POST_TAGS( ForumPostId, ForumTagId )           -- M:N junction (step 5)
    FK ForumPostId → FORUM_POSTS(Id)  [Cascade]
    FK ForumTagId  → FORUM_TAGS(Id)   [Cascade]
    PK = (ForumPostId, ForumTagId)

CONVERSATIONS( Id, ParticipantOneId(soft), ParticipantTwoId(soft),
       LastMessageAt, LastMessageId(soft), IsArchivedForParticipantOne,
       IsArchivedForParticipantTwo, {audit} )       ★UK (ParticipantOneId, ParticipantTwoId)

MESSAGES( Id, ConversationId, SenderId(soft), Body, SentAt, ReadAt,
       {audit}, {softdel} )                          CI (ConversationId, SentAt)
    FK ConversationId → CONVERSATIONS(Id)  [Cascade]

USER_BLOCKS( Id, BlockerId(soft), BlockedUserId(soft), BlockedAt, Reason )
    ★UK (BlockerId, BlockedUserId)        -- self-referencing user↔user (loose)
```

---

## 5. Resources Hub & Notifications

```
RESOURCES( Id, AuthorUserId, Slug★UK, TitleEn, TitleAr, DescriptionEn,
       DescriptionAr, ContentMarkdownEn, ContentMarkdownAr, ExternalLinkUrl,
       CoverImageUrl, AuthorRole, Type, Status, CategorySlug, TagsJson,
       IsFeatured, FeaturedOrder, PublishedAt, ReviewedAt,
       ReviewedByAdminId(soft), RejectionReason, {audit}, {softdel} )
    FK AuthorUserId → USERS(Id)        CI (Status, IsFeatured)

RESOURCE_CHAPTERS( Id, ResourceId, TitleEn, TitleAr, ContentMarkdownEn,
       ContentMarkdownAr, SortOrder, EstimatedReadMinutes )   -- weak (step 2)
    FK ResourceId → RESOURCES(Id)  [Cascade]      CI (ResourceId, SortOrder)

RESOURCE_BOOKMARKS( Id, ResourceId, UserId(soft), BookmarkedAt )
    FK ResourceId → RESOURCES(Id)  [Cascade]      ★UK (UserId, ResourceId)

RESOURCE_PROGRESS( Id, ResourceId, UserId(soft), ChaptersCompletedCount,
       LastAccessedAt, {audit} )
    FK ResourceId → RESOURCES(Id)  [Cascade]      ★UK (UserId, ResourceId)

RESOURCE_PROGRESS_CHILDREN( Id, ResourceProgressId, ResourceChildId(soft),
       IsCompleted, CompletedAt )
    FK ResourceProgressId → RESOURCE_PROGRESS(Id)  [Cascade]
    ★UK (ResourceProgressId, ResourceChildId)

NOTIFICATIONS( Id, RecipientUserId(soft), Type, Channel, TitleEn, TitleAr,
       BodyEn, BodyAr, DeepLink, MetadataJson, IsRead, ReadAt, Priority,
       IdempotencyKey, DispatchedAt, DispatchSucceeded, DispatchError,
       {audit}, {softdel} )                  CI (RecipientUserId, IsRead, CreatedAt)

NOTIFICATION_PREFERENCES( Id, UserId(soft), Type, Channel, IsEnabled, {audit} )
    ★UK (UserId, Type, Channel)
```

---

## 6. AI, Knowledge Base, Platform & Cross-cutting

```
AI_INTERACTIONS( Id, UserId(soft), Feature, Provider, ModelName, SessionId,
       PromptText, ResponseText, PromptTokens, CompletionTokens, CostUsd(14,6),
       MetadataJson, StartedAt, CompletedAt, ErrorMessage, {audit} )
       CI (UserId, StartedAt), (SessionId)

RECOMMENDATION_CLICK_EVENTS( Id, UserId, ScholarshipId, AiInteractionId,
       ClickedAt, Source )
    FK UserId          → USERS(Id)            [Restrict]
    FK ScholarshipId   → SCHOLARSHIPS(Id)     [Restrict]
    FK AiInteractionId → AI_INTERACTIONS(Id)   [SetNull]

AI_REDACTION_AUDIT_SAMPLES( Id, AiInteractionId★UK, UserId, ReviewerUserId,
       RedactedPrompt, SampledAt, Verdict, ReviewedAt )         -- 1:1 (step 3)
    FK AiInteractionId → AI_INTERACTIONS(Id)  [Restrict]   ★UK ⇒ ≤1 sample / interaction
    FK UserId          → USERS(Id)             [Restrict]
    FK ReviewerUserId  → USERS(Id)             [Restrict]   -- nullable

KNOWLEDGE_DOCUMENTS( Id, SourceType, SourceId(soft, polymorphic), SourceKey,
       TitleEn, TitleAr, ContentEn, ContentAr, ContentHash, Embedding,
       EmbeddingDimensions, EmbeddingModel, IndexedAt, MetadataJson, {audit} )
    ★UK (SourceType, SourceKey)      -- idempotent upsert key; Embedding = packed varbinary

PLATFORM_SETTINGS( Id, Key★UK, Value, ValueType, DescriptionEn, DescriptionAr,
       Category, UpdatedByAdminId(soft), {audit} )      -- EAV key/value, idx Category

AUDIT_LOGS( Id, ActorUserId(soft), Action, TargetType, TargetId(soft, polymorphic),
       BeforeJson, AfterJson, IpAddress, UserAgent, OccurredAt, CorrelationId,
       Summary )            CI (TargetType, TargetId), (ActorUserId), (OccurredAt)
    -- append-only; (TargetType,TargetId) is a polymorphic soft reference

USER_DATA_REQUESTS( Id, UserId(soft), Type, Status, RequestedAt,
       ScheduledProcessAt, CompletedAt, CancelledAt, DownloadUrl,
       DownloadExpiresAt, FailureReason, {audit} )      CI (UserId, Type, Status)

SUCCESS_STORIES( Id, StudentId(soft, nullable), AuthorDisplayName,
       AuthorImageUrl, HeadlineEn, HeadlineAr, BodyEn, BodyAr,
       ScholarshipNameEn, ScholarshipNameAr, CountryCode, IsApproved,
       IsFeatured, FeaturedOrder, {audit}, {softdel} )  CI (IsApproved, IsFeatured)
    -- PLANNED / schema-only (verified): seed data only; no admin curate command,
    -- no read query/endpoint, no client usage. Homepage feature not yet built.

USER_RISK_FLAGS( Id, UserId★UK, Score(5,4), IsAtRisk, Reason, ComputedAt,
       SourceRefreshId(soft) )                          -- 1:1 (step 3)
    FK UserId → USERS(Id)  [Restrict]   ★UK ⇒ one flag row per user
    CI (IsAtRisk, ComputedAt)
    -- ANALYTICS-DRIVEN (verified): rows are written out-of-band by the Power BI
    -- churn dataflow (no C# writer by design); the API only READS them (admin
    -- user list + CSV export, "at-risk" chip in the client).
```

---

## 7. Referential-integrity summary

| Delete rule | Used for | Why |
|---|---|---|
| **Cascade** | education(→profile), upgrade files/links(→request), scholarship-children &amp; saved-scholarships(→scholarship), application-children(→application), forum attachments/votes/flags/bookmarks/post-tags(→post/tag), messages(→conversation), resource chapters/bookmarks/progress/progress-children(→resource) | child has no meaning without its parent |
| **Restrict** | **every FK into `USERS`** (profile, refresh/reset tokens, upgrade-request, user-risk-flag, applications, documents, bookings, all reviews, recordings, recommendation clicks, redaction samples) | protect history; **and** dodge SQL Server multiple-cascade-paths (1785) |
| **SetNull** | scholarship→category, scholarship→owner-company, document→application, booking→availability/payment, request→payment, post→category | keep the row, orphan the optional link |
| **none (loose)** | payments, payouts, notifications, votes/flags/bookmarks' *user* leg, chat participants/sender, audit actor + target, AI userId, knowledge sourceId, settings/requests/stories user | decouple high-volume / audit / analytics rows from the principal so deletes & anonymisation never cascade-break |

> **Important (defense):** **no FK cascades out of `Users`.** Every `…_Users_…`
> foreign key — including the 1:1 ones on `UserProfiles`, `UserRiskFlags` and the
> `UserRoles` join — is `ON DELETE NO ACTION` (Restrict). Cascading deletes
> originate only from the *aggregate roots* listed in the Cascade row above
> (Scholarship, Application, ConsultantBooking-less child tables, ForumPost,
> Resource, Conversation, UpgradeRequest, UserProfile→Education).

## 8. Export the real SQL

```bash
cd server
dotnet ef migrations script \
  --project src/ScholarPath.Infrastructure \
  --startup-project src/ScholarPath.API \
  > ../docs/diagrams/schema.sql
```

This regenerates the authoritative DDL (all `CREATE TABLE` / `CREATE INDEX`)
straight from the migrations — a ready-to-show artifact for the defense.
