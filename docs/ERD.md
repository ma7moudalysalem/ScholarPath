# Entity Relationship Diagram

Source of truth: the EF migration files under `server/src/ScholarPath.Infrastructure/Migrations/`. This page visualizes them using Mermaid (GitHub renders it natively).

40 entities grouped by bounded context.

## Full ERD (condensed)

```mermaid
erDiagram
  ApplicationUser ||--o| UserProfile : "has"
  ApplicationUser ||--o{ RefreshToken : "issues"
  ApplicationUser ||--o{ LoginAttempt : "accumulates"
  ApplicationUser ||--o{ UpgradeRequest : "submits"
  ApplicationUser ||--o{ Scholarship : "Company owns"
  ApplicationUser ||--o{ ApplicationTracker : "Student applies"
  ApplicationUser ||--o{ ConsultantAvailability : "Consultant exposes"
  ApplicationUser ||--o{ ConsultantBooking : "Student/Consultant party"
  ApplicationUser ||--o{ CompanyReview : "Student writes"
  ApplicationUser ||--o{ ConsultantReview : "Student writes"
  ApplicationUser ||--o{ ForumPost : "authors"
  ApplicationUser ||--o{ ForumVote : "casts"
  ApplicationUser ||--o{ ForumFlag : "files"
  ApplicationUser ||--o{ ChatConversation : "participates"
  ApplicationUser ||--o{ ChatMessage : "sends"
  ApplicationUser ||--o{ UserBlock : "blocker/blocked"
  ApplicationUser ||--o{ Notification : "receives"
  ApplicationUser ||--o{ NotificationPreference : "configures"
  ApplicationUser ||--o{ AiInteraction : "starts"
  ApplicationUser ||--o{ Resource : "authors"
  ApplicationUser ||--o{ ResourceBookmark : "bookmarks"
  ApplicationUser ||--o{ ResourceProgress : "tracks"
  ApplicationUser ||--o{ SavedScholarship : "bookmarks"
  ApplicationUser ||--o{ Payment : "payer/payee"
  ApplicationUser ||--o{ Payout : "payee"
  ApplicationUser ||--o{ UserDataRequest : "requests"
  ApplicationUser ||--o{ AuditLog : "actor (nullable)"

  UserProfile ||--o{ EducationEntry : "has"

  UpgradeRequest ||--o{ UpgradeRequestFile : "uploads"
  UpgradeRequest ||--o{ UpgradeRequestLink : "attaches"

  Category ||--o{ Scholarship : "groups"
  Scholarship ||--o{ ScholarshipChild : "has rows"
  Scholarship ||--o{ SavedScholarship : "bookmarked by"
  Scholarship ||--o{ ApplicationTracker : "receives"

  ApplicationTracker ||--o{ ApplicationTrackerChild : "history/notes"
  ApplicationTracker ||--o| CompanyReview : "rated via"
  ApplicationTracker ||--o| CompanyReviewPayment : "paid for"

  ConsultantBooking ||--o| Payment : "holds"
  ConsultantBooking ||--o| ConsultantReview : "rated after"
  ConsultantAvailability ||--o{ ConsultantBooking : "optional slot"

  ForumCategory ||--o{ ForumPost : "contains"
  ForumPost ||--o{ ForumPost : "reply parent"
  ForumPost ||--o{ ForumPostAttachment : "has"
  ForumPost ||--o{ ForumVote : "gets"
  ForumPost ||--o{ ForumFlag : "flagged by"

  ChatConversation ||--o{ ChatMessage : "contains"

  Resource ||--o{ ResourceChild : "chapters"
  Resource ||--o{ ResourceBookmark : "bookmarked by"
  Resource ||--o{ ResourceProgress : "progressed by"
  ResourceProgress ||--o{ ResourceProgressChild : "per chapter"

  Payment ||--o{ Payout : "rolled into"
  ProfitShareConfig }o--|| Payment : "applied to"
  StripeWebhookEvent }o--|| Payment : "drives"
```

---

## Context view 1 — Identity + Onboarding

```mermaid
erDiagram
  ApplicationUser {
    Guid Id PK
    string Email UK
    string FirstName
    string LastName
    AccountStatus AccountStatus
    bool IsOnboardingComplete
    string ActiveRole
    string PreferredLanguage
    bool IsDeleted
    datetimeoffset CreatedAt
  }
  ApplicationRole {
    Guid Id PK
    string Name UK
    string Description
  }
  UserProfile {
    Guid Id PK
    Guid UserId FK
    string Biography
    AcademicLevel AcademicLevel
    string FieldOfStudy
    decimal Gpa
    decimal SessionFeeUsd
    int ProfileCompletenessPercent
  }
  EducationEntry {
    Guid Id PK
    Guid UserProfileId FK
    string InstitutionName
    string Degree
    DateOnly StartDate
    DateOnly EndDate
  }
  RefreshToken {
    Guid Id PK
    Guid UserId FK
    string TokenHash UK
    datetimeoffset ExpiresAt
    bool IsRevoked
    Guid ReplacedByTokenId FK
  }
  LoginAttempt {
    Guid Id PK
    string Email
    Guid UserId FK
    bool Succeeded
    datetimeoffset OccurredAt
    string IpAddress
  }
  UpgradeRequest {
    Guid Id PK
    Guid UserId FK
    UpgradeTarget Target
    UpgradeRequestStatus Status
    string Reason
    Guid ReviewedByAdminId FK
  }
  UpgradeRequestFile {
    Guid Id PK
    Guid UpgradeRequestId FK
    string FileName
    string BlobUrl
  }
  UpgradeRequestLink {
    Guid Id PK
    Guid UpgradeRequestId FK
    string Label
    string Url
  }
  ExpertiseTag {
    Guid Id PK
    string NameEn
    string NameAr
    string Slug UK
  }

  ApplicationUser ||--o| UserProfile : "1:1 cascade"
  UserProfile ||--o{ EducationEntry : "1:N cascade"
  ApplicationUser ||--o{ RefreshToken : "1:N cascade"
  ApplicationUser ||--o{ LoginAttempt : "1:N"
  ApplicationUser ||--o{ UpgradeRequest : "1:N cascade"
  UpgradeRequest ||--o{ UpgradeRequestFile : "1:N cascade"
  UpgradeRequest ||--o{ UpgradeRequestLink : "1:N cascade"
  RefreshToken ||--o| RefreshToken : "ReplacedByTokenId"
```

---

## Context view 2 — Scholarships + Applications

```mermaid
erDiagram
  Category {
    Guid Id PK
    string NameEn
    string NameAr
    string Slug UK
    int DisplayOrder
  }
  Scholarship {
    Guid Id PK
    string TitleEn
    string TitleAr
    string Slug UK
    Guid CategoryId FK
    Guid OwnerCompanyId FK
    ListingMode Mode
    string ExternalApplicationUrl
    ScholarshipStatus Status
    datetimeoffset Deadline
    FundingType FundingType
    decimal FundingAmountUsd
    AcademicLevel TargetLevel
    bool IsFeatured
    decimal ReviewFeeUsd
    bool IsDeleted
  }
  ScholarshipChild {
    Guid Id PK
    Guid ScholarshipId FK
    string ChildType
    string KeyEn
    string ValueEn
  }
  SavedScholarship {
    Guid Id PK
    Guid UserId FK
    Guid ScholarshipId FK
    string Note
  }
  ApplicationTracker {
    Guid Id PK
    Guid StudentId FK
    Guid ScholarshipId FK
    ApplicationMode Mode
    ApplicationStatus Status
    string FormDataJson
    string ExternalTrackingUrl
    bool IsReadOnly
    string PersonalNotes
  }
  ApplicationTrackerChild {
    Guid Id PK
    Guid ApplicationTrackerId FK
    string ChildType
    string Title
    string Content
    Guid ActorUserId FK
  }

  Category ||--o{ Scholarship : "SetNull on delete"
  Scholarship ||--o{ ScholarshipChild : "cascade"
  Scholarship ||--o{ SavedScholarship : "cascade"
  Scholarship ||--o{ ApplicationTracker : "Restrict"
  ApplicationTracker ||--o{ ApplicationTrackerChild : "cascade"
```

**Critical constraint** (FR-057 — Single-Active-Application rule):
```sql
CREATE UNIQUE INDEX IX_ApplicationTracker_StudentId_ScholarshipId_Active
  ON ApplicationTracker (StudentId, ScholarshipId)
  WHERE Status NOT IN ('Withdrawn', 'Rejected', 'Accepted');
```

---

## Context view 3 — Consultant booking + reviews + payments

```mermaid
erDiagram
  ConsultantAvailability {
    Guid Id PK
    Guid ConsultantId FK
    DayOfWeek DayOfWeek
    TimeOnly StartTime
    TimeOnly EndTime
    datetimeoffset SpecificStartAt
    datetimeoffset SpecificEndAt
    string Timezone
    bool IsRecurring
    bool IsActive
  }
  ConsultantBooking {
    Guid Id PK
    Guid StudentId FK
    Guid ConsultantId FK
    Guid AvailabilityId FK
    datetimeoffset ScheduledStartAt
    datetimeoffset ScheduledEndAt
    int DurationMinutes
    decimal PriceUsd
    BookingStatus Status
    string MeetingUrl
    datetimeoffset ConfirmedAt
    Guid PaymentId FK
    string StripePaymentIntentId
    bool IsNoShowStudent
    bool IsNoShowConsultant
  }
  ConsultantReview {
    Guid Id PK
    Guid BookingId FK UK
    Guid StudentId FK
    Guid ConsultantId FK
    int Rating
    string Comment
    bool IsHiddenByAdmin
  }
  CompanyReview {
    Guid Id PK
    Guid ApplicationTrackerId FK UK
    Guid StudentId FK
    Guid CompanyId FK
    int Rating
    string Comment
    bool IsHiddenByAdmin
  }
  CompanyReviewPayment {
    Guid Id PK
    Guid ApplicationTrackerId FK
    Guid CompanyId FK
    decimal AmountUsd
    decimal ProfitShareAmountUsd
    decimal PayeeAmountUsd
    string StripePaymentIntentId UK
    string IdempotencyKey UK
    PaymentStatus Status
  }
  Payment {
    Guid Id PK
    PaymentType Type
    PaymentStatus Status
    long AmountCents
    string Currency
    long ProfitShareAmountCents
    long PayeeAmountCents
    long RefundedAmountCents
    Guid PayerUserId FK
    Guid PayeeUserId FK
    string StripePaymentIntentId
    string IdempotencyKey UK
    Guid RelatedBookingId FK
    Guid RelatedApplicationId FK
  }
  Payout {
    Guid Id PK
    Guid PayeeUserId FK
    long AmountCents
    string Currency
    PayoutStatus Status
    string StripePayoutId
    string StripeConnectAccountId
  }
  StripeWebhookEvent {
    Guid Id PK
    string StripeEventId UK
    string EventType
    string RawPayload
    bool IsProcessed
    int ProcessingAttempts
  }
  ProfitShareConfig {
    Guid Id PK
    PaymentType PaymentType
    decimal Percentage
    datetimeoffset EffectiveFrom
    datetimeoffset EffectiveTo
    Guid SetByAdminId FK
  }

  ConsultantAvailability ||--o{ ConsultantBooking : "optional slot"
  ConsultantBooking ||--o| ConsultantReview : "rated once"
  ConsultantBooking ||--o| Payment : "linked (nullable)"
  ApplicationTracker ||--o| CompanyReview : "rated once"
  Payment ||--o{ Payout : "settles into"
```

---

## Context view 4 — Community + Chat + Resources + AI

```mermaid
erDiagram
  ForumCategory {
    Guid Id PK
    string NameEn
    string NameAr
    string Slug UK
    int DisplayOrder
  }
  ForumPost {
    Guid Id PK
    Guid AuthorId FK
    Guid CategoryId FK
    Guid ParentPostId FK
    string Title
    string BodyMarkdown
    PostModerationStatus ModerationStatus
    int UpvoteCount
    int DownvoteCount
    int FlagCount
    bool IsAutoHidden
  }
  ForumPostAttachment {
    Guid Id PK
    Guid ForumPostId FK
    string FileName
    string BlobUrl
  }
  ForumVote {
    Guid Id PK
    Guid ForumPostId FK
    Guid UserId FK
    VoteType VoteType
  }
  ForumFlag {
    Guid Id PK
    Guid ForumPostId FK
    Guid FlaggedByUserId FK
    string Reason
    bool IsValid
  }
  ChatConversation {
    Guid Id PK
    Guid ParticipantOneId FK
    Guid ParticipantTwoId FK
    datetimeoffset LastMessageAt
  }
  ChatMessage {
    Guid Id PK
    Guid ConversationId FK
    Guid SenderId FK
    string Body
    datetimeoffset SentAt
    datetimeoffset ReadAt
    bool IsDeleted
  }
  UserBlock {
    Guid Id PK
    Guid BlockerId FK
    Guid BlockedUserId FK
    string Reason
  }
  Resource {
    Guid Id PK
    string TitleEn
    string TitleAr
    string Slug UK
    string ContentMarkdownEn
    string ContentMarkdownAr
    Guid AuthorUserId FK
    string AuthorRole
    ResourceType Type
    ResourceStatus Status
    bool IsFeatured
  }
  ResourceChild {
    Guid Id PK
    Guid ResourceId FK
    string TitleEn
    string TitleAr
    int SortOrder
  }
  ResourceBookmark {
    Guid Id PK
    Guid UserId FK
    Guid ResourceId FK
  }
  ResourceProgress {
    Guid Id PK
    Guid UserId FK
    Guid ResourceId FK
    int ChaptersCompletedCount
  }
  ResourceProgressChild {
    Guid Id PK
    Guid ResourceProgressId FK
    Guid ResourceChildId FK
    bool IsCompleted
  }
  AiInteraction {
    Guid Id PK
    Guid UserId FK
    AiFeature Feature
    AiProvider Provider
    string ModelName
    string SessionId
    string PromptText
    string ResponseText
    int PromptTokens
    int CompletionTokens
    decimal CostUsd
  }

  ForumCategory ||--o{ ForumPost : "contains"
  ForumPost ||--o{ ForumPost : "replies"
  ForumPost ||--o{ ForumPostAttachment : "cascade"
  ForumPost ||--o{ ForumVote : "cascade, unique(post,user)"
  ForumPost ||--o{ ForumFlag : "cascade, unique(post,user)"
  ChatConversation ||--o{ ChatMessage : "cascade"
  Resource ||--o{ ResourceChild : "cascade"
  Resource ||--o{ ResourceBookmark : "unique(user,resource)"
  Resource ||--o{ ResourceProgress : "unique(user,resource)"
  ResourceProgress ||--o{ ResourceProgressChild : "unique(progress,child)"
```

---

## Context view 5 — Cross-cutting (Notifications, Audit, DataRequests)

```mermaid
erDiagram
  Notification {
    Guid Id PK
    Guid RecipientUserId FK
    NotificationType Type
    NotificationChannel Channel
    string TitleEn
    string TitleAr
    string BodyEn
    string BodyAr
    string DeepLink
    bool IsRead
    datetimeoffset ReadAt
    string IdempotencyKey
    int Priority
    bool DispatchSucceeded
  }
  NotificationPreference {
    Guid Id PK
    Guid UserId FK
    NotificationType Type
    NotificationChannel Channel
    bool IsEnabled
  }
  AuditLog {
    Guid Id PK
    Guid ActorUserId FK
    AuditAction Action
    string TargetType
    Guid TargetId
    string BeforeJson
    string AfterJson
    string IpAddress
    string CorrelationId
    datetimeoffset OccurredAt
  }
  UserDataRequest {
    Guid Id PK
    Guid UserId FK
    UserDataRequestType Type
    UserDataRequestStatus Status
    datetimeoffset RequestedAt
    datetimeoffset ScheduledProcessAt
    string DownloadUrl
    datetimeoffset DownloadExpiresAt
  }
  SuccessStory {
    Guid Id PK
    Guid StudentId FK
    string AuthorDisplayName
    string HeadlineEn
    string HeadlineAr
    string BodyEn
    string BodyAr
    bool IsApproved
    bool IsFeatured
  }

  ApplicationUser ||--o{ Notification : "recipient"
  ApplicationUser ||--o{ NotificationPreference : "unique(user,type,channel)"
  ApplicationUser ||--o{ AuditLog : "actor (nullable for system)"
  ApplicationUser ||--o{ UserDataRequest : "owner"
  ApplicationUser ||--o{ SuccessStory : "optional author"
```

---

## Legend

| Notation | Meaning |
|---|---|
| `PK` | Primary key |
| `FK` | Foreign key |
| `UK` | Unique key / constraint |
| `\|\|--o{` | 1-to-many (zero-or-more) |
| `\|\|--o\|` | 1-to-0-or-1 |
| `}o--\|\|` | many-to-1 |
| `}o--o{` | many-to-many (we use join tables explicitly) |

---

## How to regenerate

The ERD is hand-written from the Domain entities. If you add an entity:

1. Add its class to `server/src/ScholarPath.Domain/Entities/`.
2. Add its `IEntityTypeConfiguration<T>` under `server/src/ScholarPath.Infrastructure/Persistence/Configurations/`.
3. Register the DbSet in `ApplicationDbContext` + `IApplicationDbContext`.
4. Run `dotnet ef migrations add <FeatureName> --project src/ScholarPath.Infrastructure --startup-project src/ScholarPath.API`.
5. Update this ERD file with the new entity and relationships.

To export SQL for the defense:
```bash
cd server
dotnet ef migrations script --project src/ScholarPath.Infrastructure --startup-project src/ScholarPath.API > ../docs/schema.sql
```
