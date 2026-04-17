# ERD Mapping

Entity → SQL table → keys → indexes → constraints.

The **authoritative schema** is the EF migration at:
`server/src/ScholarPath.Infrastructure/Migrations/20260417164008_InitialSchema.cs`.

This page maps each Domain entity to its persistence representation.

## Legend

- **PK** primary key  •  **FK** foreign key  •  **UK** unique index
- **FQF** filtered unique index (condition in SQL)
- **SD** soft-delete via `IsDeleted` + EF global query filter
- **RV** optimistic concurrency via `RowVersion` column
- **CI** composite index (multi-column)

## Identity + Auth

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `ApplicationUser` | `Users` | PK `Id`, UK `NormalizedEmail`, CI `(AccountStatus)`, CI `(IsOnboardingComplete)` | SD, RV | Renamed from `AspNetUsers`. Extends `IdentityUser<Guid>`. |
| `ApplicationRole` | `Roles` | PK `Id`, UK `NormalizedName` | — | 5 seeded: Admin, Student, Company, Consultant, Unassigned |
| Identity join tables | `UserRoles`, `UserClaims`, `UserLogins`, `UserTokens`, `RoleClaims` | standard | — | Renamed from `AspNetUserXxx`. |
| `UserProfile` | `UserProfiles` | PK `Id`, UK `UserId` (1:1), decimal precision on `Gpa (4,2)`, `SessionFeeUsd (10,2)` | — | Cascade-delete with user. |
| `EducationEntry` | `EducationEntries` | PK `Id`, FK `UserProfileId` (cascade) | — | Multi-row history. |
| `ExpertiseTag` | `ExpertiseTags` | PK `Id`, UK `Slug` | — | Consultant expertise taxonomy. |
| `RefreshToken` | `RefreshTokens` | PK `Id`, UK `TokenHash`, CI `(UserId, IsRevoked)` | RV | Replacement chain via `ReplacedByTokenId` (self-FK, nullable). |
| `LoginAttempt` | `LoginAttempts` | PK `Id`, CI `(Email, OccurredAt)` | — | Append-only. Drives lockout policy. |

## Onboarding / Role upgrade

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `UpgradeRequest` | `UpgradeRequests` | PK `Id`, CI `(UserId, Status)` | SD, RV | State machine: Pending → Approved / Rejected / Cancelled. |
| `UpgradeRequestFile` | `UpgradeRequestFiles` | PK, FK cascade | — | Supporting docs (Blob URLs). |
| `UpgradeRequestLink` | `UpgradeRequestLinks` | PK, FK cascade | — | Supporting links (LinkedIn, portfolio). |

## Scholarships

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `Category` | `Categories` | PK `Id`, UK `Slug` | RV | EN + AR names. |
| `Scholarship` | `Scholarships` | PK `Id`, UK `Slug`, CI `(Status, Deadline)`, CI `(IsFeatured)`, CI `(Mode)`, FK `CategoryId` (SetNull), FK `OwnerCompanyId` (SetNull) | SD, RV | Decimal precision `FundingAmountUsd(14,2)`, `ReviewFeeUsd(10,2)`. |
| `ScholarshipChild` | `ScholarshipChildren` | PK `Id`, FK cascade, CI `(ScholarshipId, ChildType)` | — | Generic child row (requirements, benefits, etc.). |
| `SavedScholarship` | `SavedScholarships` | PK `Id`, **UK** `(UserId, ScholarshipId)` | — | Student bookmark. One per pair. |

## Applications

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `ApplicationTracker` | `Applications` | PK `Id`, **FQF** `(StudentId, ScholarshipId) WHERE Status NOT IN ('Withdrawn','Rejected','Accepted')`, CI `(Status)`, FK `StudentId` (Restrict), FK `ScholarshipId` (Restrict) | SD, RV | **FR-057 single-active rule enforced at SQL.** Transitions locked on final outcomes. |
| `ApplicationTrackerChild` | `ApplicationChildren` | PK `Id`, FK cascade, CI `(ApplicationTrackerId, ChildType)` | — | Status history + notes + task rows. |

**Critical SQL** (in the migration):
```sql
CREATE UNIQUE INDEX IX_Applications_StudentId_ScholarshipId_Active
  ON [Applications] ([StudentId], [ScholarshipId])
  WHERE [Status] NOT IN ('Withdrawn', 'Rejected', 'Accepted');
```

## Company review + Consultant review

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `CompanyReview` | `CompanyReviews` | PK, **UK** `ApplicationTrackerId`, CI `(CompanyId, IsHiddenByAdmin, IsDeleted)` | SD, RV | One review per finalized application. |
| `CompanyReviewPayment` | `CompanyReviewPayments` | PK, UK `StripePaymentIntentId`, UK `IdempotencyKey`, decimal precision `(14,2)` on amounts | RV | Linked to `ApplicationTracker`. |
| `ConsultantReview` | `ConsultantReviews` | PK, **UK** `BookingId`, CI `(ConsultantId, IsHiddenByAdmin, IsDeleted)` | SD, RV | One review per booking. |

## Consultant booking

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `ConsultantAvailability` | `Availabilities` | PK, CI `(ConsultantId, IsActive)` | SD, RV | Either recurring (DayOfWeek + StartTime + EndTime) or ad-hoc (SpecificStartAt + SpecificEndAt). UTC + Timezone. |
| `ConsultantBooking` | `Bookings` | PK, CI `(ConsultantId, ScheduledStartAt)`, CI `(StudentId, Status)`, CI `(Status)`, FK `StudentId` (Restrict), FK `ConsultantId` (Restrict), FK `PaymentId` (SetNull) | SD, RV | 8-state machine. Decimal `PriceUsd(10,2)`. `StripePaymentIntentId` max 256. |

## Payments + settlement

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `Payment` | `Payments` | PK, **UK** `IdempotencyKey`, CI `(StripePaymentIntentId)`, CI `(PayerUserId, Status)`, CI `(PayeeUserId, Status)` | SD, RV | **Amounts in cents** (integer) to avoid FP rounding. ProfitShare snapshot stored — never recomputed. |
| `Payout` | `Payouts` | PK, CI `(PayeeUserId, Status)` | RV | Aggregates N Payments (via `IncludedPaymentIdsJson`) into one Stripe Connect transfer. |
| `StripeWebhookEvent` | `StripeWebhookEvents` | PK, **UK** `StripeEventId`, CI `(IsProcessed, ReceivedAt)` | — | Idempotency log. Replays are no-ops. Raw payload retained. |
| `ProfitShareConfig` | `ProfitShareConfigs` | PK, CI `(PaymentType, EffectiveTo)`, decimal `Percentage(5,4)` (e.g., 0.1000 = 10%) | RV | Only one active per `PaymentType` at a time (enforced in handler). Admin-managed. |

## Community + Chat

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `ForumCategory` | `ForumCategories` | PK, UK `Slug` | RV | |
| `ForumPost` | `ForumPosts` | PK, CI `(CategoryId, CreatedAt)`, CI `(ParentPostId)`, CI `(IsAutoHidden)`, FK `AuthorId` (Restrict), FK `ParentPostId` (Restrict self) | SD, RV | Threads = root; replies have `ParentPostId`. Auto-hidden at 3+ distinct valid flags (FR-107). |
| `ForumPostAttachment` | `ForumPostAttachments` | PK, FK cascade | — | Blob URLs. |
| `ForumVote` | `ForumVotes` | PK, **UK** `(ForumPostId, UserId)` | — | One vote per user per post. Self-voting blocked in handler. |
| `ForumFlag` | `ForumFlags` | PK, **UK** `(ForumPostId, FlaggedByUserId)` | — | One flag per user per post. `IsValid` can be cleared by admin. |
| `ChatConversation` | `Conversations` | PK, **UK** `(ParticipantOneId, ParticipantTwoId)`, CI `(LastMessageAt)` | RV | 1:1 only in v1. Lexicographical participant ordering. |
| `ChatMessage` | `Messages` | PK, CI `(ConversationId, SentAt)` | SD, RV | Cascade from conversation. |
| `UserBlock` | `UserBlocks` | PK, **UK** `(BlockerId, BlockedUserId)` | — | Blocked user cannot start new conversation. |

## Resources Hub

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `Resource` | `Resources` | PK, UK `Slug`, CI `(Status, IsFeatured)`, CI `(AuthorUserId)` | SD, RV | EN + AR content + optional chapters. |
| `ResourceChild` | `ResourceChapters` | PK, CI `(ResourceId, SortOrder)` | — | Optional chapter breakdown. |
| `ResourceBookmark` | `ResourceBookmarks` | PK, **UK** `(UserId, ResourceId)` | — | |
| `ResourceProgress` | `ResourceProgress` | PK, **UK** `(UserId, ResourceId)` | RV | Chapter-level tracking. |
| `ResourceProgressChild` | `ResourceProgressChildren` | PK, **UK** `(ResourceProgressId, ResourceChildId)` | — | |

## AI interactions

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `AiInteraction` | `AiInteractions` | PK, CI `(UserId, StartedAt)`, CI `(SessionId)`, decimal `CostUsd(14,6)` | RV | Telemetry + cost accounting per feature (recommender, eligibility, chatbot). |

## Notifications

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `Notification` | `Notifications` | PK, CI `(RecipientUserId, IsRead, CreatedAt)`, CI `IdempotencyKey` | SD, RV | In-app + email channels. Bilingual title + body. |
| `NotificationPreference` | `NotificationPreferences` | PK, **UK** `(UserId, Type, Channel)` | RV | Per-user per-type per-channel opt-in/out. |

## Cross-cutting

| Entity | Table | Keys / Indexes | Soft-delete | Notes |
|---|---|---|---|---|
| `AuditLog` | `AuditLogs` | PK, CI `(TargetType, TargetId)`, CI `(ActorUserId)`, CI `(OccurredAt)` | — | Append-only. Written by the `[Auditable]` MediatR behavior. |
| `UserDataRequest` | `UserDataRequests` | PK, CI `(UserId, Type, Status)` | RV | GDPR export/delete with 30-day cooling. |
| `SuccessStory` | `SuccessStories` | PK, CI `(IsApproved, IsFeatured)` | SD, RV | Curated stories for the public home page. |

## Global conventions

| Convention | Applied to |
|---|---|
| All PKs are `Guid` (SQL `uniqueidentifier`) | every entity |
| All timestamps are `datetimeoffset` (UTC) | every `CreatedAt`, `UpdatedAt`, etc. |
| All business-entity tables have `RowVersion` | optimistic concurrency |
| Soft delete via `IsDeleted` + EF `HasQueryFilter` | every `ISoftDeletable` entity |
| Enums serialized as **strings** | via `HasConversion<string>()` (readable in DB) |
| Strings with semantic meaning capped at `HasMaxLength` | every string column |
| Domain events dispatched **after** `SaveChangesAsync` | via `ApplicationDbContext.DispatchDomainEventsAsync` |

## Foreign-key delete semantics

| Relationship | Delete behavior | Rationale |
|---|---|---|
| User → Profile | Cascade | Profile has no meaning without user. |
| User → RefreshTokens | Cascade | Tokens are session-scoped. |
| User → Scholarships (company owns) | SetNull | Preserve scholarship, orphan owner. |
| Student → ApplicationTracker | Restrict | Can't delete student with open applications. |
| Booking → Payment | SetNull | Audit the payment even if booking removed. |
| ApplicationTracker → CompanyReview | Cascade | Review is meaningless without the application. |
| ForumPost → ForumPost (reply) | Restrict | Protect thread integrity. |
| ForumCategory → ForumPost | SetNull | Move orphaned posts to "uncategorized". |

## How to export to SQL

```bash
cd server
dotnet ef migrations script \
  --project src/ScholarPath.Infrastructure \
  --startup-project src/ScholarPath.API \
  > ../docs/schema.sql
```

For the grad defense, the `schema.sql` file is a ready-to-show artifact.
