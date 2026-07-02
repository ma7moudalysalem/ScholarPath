# ScholarPath — Enhanced Entity–Relationship Diagram (EERD)

> **Source of truth:** the live EF Core model snapshot
> `server/src/ScholarPath.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`
> (table names, keys, indexes) plus the domain entities under
> `server/src/ScholarPath.Domain/Entities/`. This document supersedes the older
> `docs/ERD-MAPPING.md`, which predates several merged modules.
>
> **Notation:** the *conceptual* model uses **Chen / Elmasri** notation (see the
> PlantUML `@startchen` sources in `plantuml/` for the textbook rectangles,
> diamonds and ellipses). The diagrams **inline below** are the same model in
> **Mermaid (crow's-foot)** so they render directly on GitHub and in the report.
> The legend maps one notation onto the other.

---

## 0. The EERD in authentic Chen / Elmasri notation (high-resolution)

The diagram below is the **conceptual EERD in textbook Chen notation** —
rectangles = entity types, diamonds = relationship types, ellipses =
attributes, underlined = key attributes, `1 / N` = cardinality ratios — rendered
from [`plantuml/eerd-chen-core.puml`](plantuml/eerd-chen-core.puml) as **vector
SVG** (open it and zoom in losslessly — no pixelation).

![ScholarPath EERD — Chen / Elmasri notation](img/ScholarPath_EERD_Core.svg)

> The per-cluster diagrams in §4–§9 are a **crow's-foot detail companion** that
> list every entity + key attributes; the Chen diagram above is the conceptual
> master in the exact notation used by Elmasri & Navathe. All images here are
> now vector **SVG** (sharp at any zoom).

---

## 1. Notation legend (Elmasri ↔ crow's-foot)

| Concept (Elmasri / Chen) | Chen symbol | Crow's-foot (Mermaid) used here |
|---|---|---|
| Entity type | rectangle | named box `USERS { … }` |
| Weak entity type | double rectangle | box whose identity depends on a parent (noted *(weak)*) |
| Relationship type | diamond | the line between two boxes (verb label) |
| Attribute / key | ellipse / underlined ellipse | `PK`, `FK`, `UK` markers inside the box |
| Cardinality ratio 1:1 / 1:N / M:N | `1`, `N`, `M` on the edges | `||`, `o{`, `}o` crow's-foot ends |
| **Total** participation (mandatory) | **double line** | mandatory end `||` (one) / `}|` (one-or-many) |
| **Partial** participation (optional) | single line | optional end `|o` (zero-or-one) / `o{` (zero-or-many) |
| Specialization / generalization (ISA) | circle + subclass lines (EER) | shown separately in §3 (Mermaid can't draw the EER specialization **circle** with the d/o + total/partial markers — the authentic version is `img/eer-specialization.svg`) |

**One extra convention specific to this codebase — and it is important:**

| Line style | Meaning |
|---|---|
| **Solid** line (`--`) | a **database-enforced foreign key** (real `FOREIGN KEY` constraint with a declared delete rule). |
| **Dashed** line (`..`) | an **application-enforced "loose" reference** — a `Guid` column that *semantically* points at another row but has **no FK constraint** in the schema. Referential integrity is guaranteed by application code, not the database. |

The platform deliberately uses loose references for high-volume / audit / log /
analytics tables (notifications, votes, chat, audit log, AI interactions, …) so
that deleting or anonymising a user never cascade-breaks history. Modelling them
honestly is part of an accurate EERD.

> **Note for examiners:** in *standard* crow's-foot, a solid line is an
> *identifying* relationship and a dashed line a *non-identifying* one. Here we
> deliberately **repurpose** that visual distinction to mean **DB-enforced FK
> (solid)** vs **application-enforced loose reference (dashed)** — a project-
> specific convention, stated up front so the diagrams can't be misread.

---

## 2. Subject-area context (the big picture)

![Subject-area context](img/eerd-00-context.svg)

Everything radiates from **USERS** (the single ASP.NET Identity principal that
represents Students, Companies, Consultants and Admins — see §3). Each box below
is a subject area detailed in its own ER diagram (§4–§9).

```mermaid
flowchart TB
    U(("USERS<br/>Student · Company<br/>Consultant · Admin"))

    subgraph IDP["§4 Identity & Profile"]
        P[UserProfile · Education<br/>RefreshToken · UpgradeRequest]
    end
    subgraph SCH["§5 Scholarships · Applications · Documents"]
        S[Scholarship · Application<br/>Document · CompanyReview]
    end
    subgraph BPR["§6 Booking · Payments · Ratings"]
        B[Availability · Booking · Payment<br/>Payout · ConsultantReview · Recording]
    end
    subgraph COM["§7 Community & Chat"]
        C[ForumPost · Vote · Flag · Tag<br/>Conversation · Message]
    end
    subgraph RES["§8 Resources & Notifications"]
        R[Resource · Chapter · Progress<br/>Notification · Preference]
    end
    subgraph AIX["§9 AI · Knowledge · Platform"]
        A[AiInteraction · KnowledgeDoc<br/>AuditLog · Setting · RiskFlag]
    end

    U --- IDP
    U --- SCH
    U --- BPR
    U --- COM
    U --- RES
    U --- AIX
    SCH -. paid review .-> BPR
```

---

## 3. EER specialization — the USER super-type

![EER specialization of USER](img/eerd-01-user-specialization.svg)

This is the **"Enhanced"** part of the EERD. Conceptually, a **USER** is a
super-type specialized into four sub-types by the role(s) granted to it:

```mermaid
classDiagram
    class USER {
        <<super-type>>
        +Guid Id
        +string Email
        +AccountStatus AccountStatus
        +string ActiveRole
    }
    class STUDENT {
        <<role : academic>>
        +AcademicLevel academicLevel
        +string fieldOfStudy
        +decimal gpa
        +string[] preferredCountries
    }
    class COMPANY {
        <<role : organization>>
        +string organizationLegalName
        +string verificationStatus
        +decimal companyAverageRating
    }
    class CONSULTANT {
        <<role : expert>>
        +decimal sessionFeeUsd
        +string[] expertiseTags
        +StripeConnectStatus stripeConnectStatus
    }
    class ADMIN {
        <<role : operator>>
    }
    USER <|-- STUDENT : ISA
    USER <|-- COMPANY : ISA
    USER <|-- CONSULTANT : ISA
    USER <|-- ADMIN : ISA
```

**Specialization constraints (EER):**

- **Overlapping `{o}`** — a user may hold **more than one** role at a time
  (dual-role: a Student can be upgraded to Consultant and keep both). This is why
  it is *not* a disjoint specialization.
- **Partial** — a freshly-registered user is `Unassigned` and belongs to **no**
  sub-type until role selection / onboarding completes.

**How this maps to the physical schema (code reality):**

- There is **no table-per-subclass**. The super-type is the `Users` table; role
  membership is the **`UserRoles`** join (ASP.NET Identity many-to-many to
  `Roles`). The seeded roles are exactly `Admin, Student, Company, Consultant,
  Unassigned`.
- The role-specific attributes (Student academic fields, Company organization
  fields, Consultant expert fields) are **all columns on the single
  `UserProfiles` table** — a *single-table* realization of the specialization,
  with the columns of the inapplicable sub-types left `NULL`.

So §3 is the conceptual EER view; §4 shows its physical realization.

---

## 4. Identity, Access & Profile

**Chen / Elmasri notation** (authentic book symbols):

![EERD Chen — Identity & Profile](img/EERD_Identity_Profile.svg)

**Crow's-foot detail companion** (every entity + key attributes):

![EERD — Identity, Access & Profile](img/eerd-02-identity-profile.svg)

```mermaid
erDiagram
    USERS ||--o{ USER_ROLES : "granted"
    ROLES ||--o{ USER_ROLES : "granted to"
    USERS ||--o| USER_PROFILES : "has (1:1)"
    USER_PROFILES ||--o{ EDUCATION_ENTRIES : "lists"
    USERS ||--o{ REFRESH_TOKENS : "issues"
    USERS ||--o{ PASSWORD_RESET_TOKENS : "requests"
    USERS ||..o{ LOGIN_ATTEMPTS : "attempts (loose)"
    USERS ||--o{ UPGRADE_REQUESTS : "submits"
    USERS ||..o{ UPGRADE_REQUESTS : "reviews-admin (loose)"
    UPGRADE_REQUESTS ||--o{ UPGRADE_REQUEST_FILES : "attaches"
    UPGRADE_REQUESTS ||--o{ UPGRADE_REQUEST_LINKS : "attaches"
    REFRESH_TOKENS ||..o| REFRESH_TOKENS : "replaced-by (loose)"

    USERS {
        guid Id PK
        string Email "app-unique (no DB UK)"
        string FirstName
        string LastName
        string AccountStatus "Unassigned|PendingApproval|Active|Suspended|Deactivated"
        string ActiveRole
        bool IsOnboardingComplete
        bool IsDeleted "soft-delete"
    }
    ROLES {
        guid Id PK
        string Name "Admin|Student|Company|Consultant|Unassigned"
    }
    USER_ROLES {
        guid UserId PK "FK -> Users"
        guid RoleId PK "FK -> Roles"
    }
    USER_PROFILES {
        guid Id PK
        guid UserId FK "UK, 1:1 -> Users"
        string Biography "encrypted"
        string AcademicLevel
        decimal Gpa
        string OrganizationLegalName
        decimal SessionFeeUsd
        string StripeConnectStatus
        decimal CompanyAverageRating
        int ProfileCompletenessPercent
    }
    EDUCATION_ENTRIES {
        guid Id PK
        guid UserProfileId FK
        string InstitutionName
        string Degree
    }
    REFRESH_TOKENS {
        guid Id PK
        guid UserId FK
        string TokenHash UK
        datetimeoffset ExpiresAt
        guid ReplacedByTokenId "loose self-ref"
    }
    PASSWORD_RESET_TOKENS {
        guid Id PK
        guid UserId FK
        string TokenHash UK
    }
    LOGIN_ATTEMPTS {
        guid Id PK
        guid UserId "loose ref"
        string Email
        bool Succeeded
    }
    UPGRADE_REQUESTS {
        guid Id PK
        guid UserId FK "Restrict (real FK)"
        guid ReviewedByAdminId "loose ref"
        string Target "Company|Consultant"
        string Status "Pending|Approved|Rejected|Cancelled"
    }
    UPGRADE_REQUEST_FILES {
        guid Id PK
        guid UpgradeRequestId FK
        string BlobUrl
    }
    UPGRADE_REQUEST_LINKS {
        guid Id PK
        guid UpgradeRequestId FK
        string Url
    }
    EXPERTISE_TAGS {
        guid Id PK
        string Slug UK
        string NameEn
        string NameAr
    }
```

> `EXPERTISE_TAGS` is a standalone taxonomy table; consultant profiles reference
> it only through the denormalized `UserProfiles.ExpertiseTagsJson` string, so
> there is **no** relationship edge to it.

> **First-time onboarding vs. role upgrade (verified against code).**
> First-time **Company / Consultant onboarding** (SRS FR-ONB-03..07) is **not**
> modelled by `UPGRADE_REQUESTS`. It is realized by the *role-selection* flow:
> the onboarding payload is written onto `USER_PROFILES`, the principal's
> `USERS.AccountStatus` moves to `PendingApproval`, the admin queue reads
> `USERS WHERE AccountStatus = PendingApproval`, and the verification files are
> stored as `DOCUMENTS` with `Category = OnboardingDocument`. `UPGRADE_REQUESTS`
> (+ its `*_FILES` / `*_LINKS` children) backs **only the post-onboarding
> Student→Consultant role upgrade**; the `Target = Company` value and the file/
> link child tables exist in the schema/seed but are **not exercised by any live
> handler** (vestigial).

---

## 5. Scholarships, Applications & Documents

**Chen / Elmasri notation** (authentic book symbols):

![EERD Chen — Scholarships & Applications](img/EERD_Scholarships_Applications.svg)

**Crow's-foot detail companion** (every entity + key attributes):

![EERD — Scholarships, Applications & Documents](img/eerd-03-scholarships-applications.svg)

```mermaid
erDiagram
    CATEGORIES ||--o{ SCHOLARSHIPS : "classifies"
    USERS ||--o{ SCHOLARSHIPS : "owns (company)"
    SCHOLARSHIPS ||--o{ SCHOLARSHIP_CHILDREN : "details"
    USERS ||..o{ SAVED_SCHOLARSHIPS : "bookmarks (loose user)"
    SCHOLARSHIPS ||--o{ SAVED_SCHOLARSHIPS : "bookmarked-in"
    USERS ||--o{ APPLICATIONS : "submits (student)"
    SCHOLARSHIPS ||--o{ APPLICATIONS : "receives"
    APPLICATIONS ||--o{ APPLICATION_CHILDREN : "history"
    USERS ||--o{ DOCUMENTS : "owns"
    APPLICATIONS ||--o{ DOCUMENTS : "supported-by"
    USERS ||--o{ COMPANY_REVIEW_REQUESTS : "raises (student)"
    USERS ||--o{ COMPANY_REVIEW_REQUESTS : "fulfils (company)"
    SCHOLARSHIPS ||--o{ COMPANY_REVIEW_REQUESTS : "under"
    PAYMENTS ||--o| COMPANY_REVIEW_REQUESTS : "settles"
    APPLICATIONS ||--o| COMPANY_REVIEWS : "rated-by (1:1)"
    USERS ||--o{ COMPANY_REVIEWS : "authors (student)"
    USERS ||--o{ COMPANY_REVIEWS : "rated (company)"

    SCHOLARSHIPS {
        guid Id PK
        guid CategoryId FK "SetNull"
        guid OwnerCompanyId FK "SetNull, null=admin/external"
        string Slug UK
        string TitleEn
        string Mode "InApp|ExternalUrl"
        string Status "Draft|UnderReview|Open|Closed|Archived"
        datetimeoffset Deadline
        decimal ReviewFeeUsd
    }
    CATEGORIES {
        guid Id PK
        string Slug UK
    }
    SCHOLARSHIP_CHILDREN {
        guid Id PK
        guid ScholarshipId FK "Cascade (weak entity)"
        string ChildType
    }
    SAVED_SCHOLARSHIPS {
        guid Id PK
        guid UserId "loose ref (UK pair)"
        guid ScholarshipId FK "Cascade (UK pair)"
    }
    APPLICATIONS {
        guid Id PK
        guid StudentId FK "Restrict"
        guid ScholarshipId FK "Restrict, null=external"
        string Mode "InApp|External"
        string Status "Draft|Pending|UnderReview|Shortlisted|Accepted|Rejected|Withdrawn|Intending|Applied|WaitingResult"
        string PersonalNotes "encrypted"
    }
    APPLICATION_CHILDREN {
        guid Id PK
        guid ApplicationTrackerId FK "Cascade (weak entity)"
        guid ActorUserId "loose ref"
        string ChildType "StatusHistory|Note|Task"
    }
    DOCUMENTS {
        guid Id PK
        guid OwnerUserId FK "Restrict"
        guid ApplicationTrackerId FK "SetNull"
        string Category
        string StoragePath
    }
    COMPANY_REVIEW_REQUESTS {
        guid Id PK
        guid StudentId FK "Restrict"
        guid CompanyId FK "Restrict"
        guid ScholarshipId FK "Restrict"
        guid PaymentId FK "SetNull"
        string Status "Draft..Completed/Closed/Cancelled..."
        decimal ReviewFeeUsdSnapshot
    }
    COMPANY_REVIEWS {
        guid Id PK
        guid ApplicationTrackerId FK "UK, 1:1 Restrict"
        guid StudentId FK "Restrict"
        guid CompanyId FK "Restrict"
        int Rating "1..5"
    }
```

> **Single-active rule (FR-057 / FR-APP-03)** for in-app applications is
> enforced **in application code only** — there is **no** DB filtered-unique
> index on `Applications` in production (the old
> `UX_Applications_Student_Scholarship_Active` was dropped when `ScholarshipId`
> became nullable and was never recreated; live `Applications` carries only the
> non-unique `IX_Applications_ScholarshipId` and `IX_Applications_Status`).
> By contrast, the analogous **filtered-unique index on
> `CompanyReviewRequests(StudentId, ScholarshipId)` DOES exist** and DB-enforces
> one live paid-review request at a time.

---

## 6. Consultant Booking, Payments, Ratings & Recording

**Chen / Elmasri notation** (authentic book symbols):

![EERD Chen — Booking, Payments & Ratings](img/EERD_Booking_Payments_Ratings.svg)

**Crow's-foot detail companion** (every entity + key attributes):

![EERD — Booking, Payments, Ratings & Recording](img/eerd-04-booking-payments-ratings.svg)

```mermaid
erDiagram
    USERS ||--o{ AVAILABILITIES : "publishes (consultant)"
    USERS ||--o{ BOOKINGS : "books (student)"
    USERS ||--o{ BOOKINGS : "conducts (consultant)"
    AVAILABILITIES ||--o{ BOOKINGS : "reserves"
    BOOKINGS ||--o| PAYMENTS : "settled-by"
    BOOKINGS ||--o| CONSULTANT_REVIEWS : "rated-by (1:1)"
    USERS ||--o{ CONSULTANT_REVIEWS : "authors (student)"
    USERS ||--o{ CONSULTANT_REVIEWS : "rated (consultant)"
    BOOKINGS ||--o{ SESSION_RECORDINGS : "recorded-as"
    USERS ||..o{ PAYMENTS : "payer (loose)"
    USERS ||..o{ PAYMENTS : "payee (loose)"
    PAYOUTS ||..o{ PAYMENTS : "aggregates (loose+JSON)"

    AVAILABILITIES {
        guid Id PK
        guid ConsultantId FK "Restrict"
        bool IsRecurring
        bool IsActive
        string Timezone
    }
    BOOKINGS {
        guid Id PK
        guid StudentId FK "Restrict"
        guid ConsultantId FK "Restrict"
        guid AvailabilityId FK "SetNull"
        guid PaymentId FK "SetNull"
        datetimeoffset ScheduledStartAt
        decimal PriceUsd
        string Status "Requested|Confirmed|Rejected|Expired|Cancelled|Completed|NoShow*"
    }
    PAYMENTS {
        guid Id PK
        guid PayerUserId "loose ref"
        guid PayeeUserId "loose ref"
        guid RelatedBookingId "loose ref"
        guid RelatedApplicationId "loose ref"
        guid PayoutId "loose ref"
        string Type "ConsultantBooking|CompanyReview"
        string Status "Pending|Held|Captured|PartiallyRefunded|Refunded|Failed|Cancelled|Disputed"
        long AmountCents
        string IdempotencyKey UK
    }
    PAYOUTS {
        guid Id PK
        guid PayeeUserId "loose ref"
        long AmountCents
        string Status "Pending|InTransit|Paid|Failed"
        string IncludedPaymentIdsJson
    }
    CONSULTANT_REVIEWS {
        guid Id PK
        guid BookingId FK "UK, 1:1 Restrict"
        guid StudentId FK "Restrict"
        guid ConsultantId FK "Restrict"
        int Rating "1..5"
    }
    SESSION_RECORDINGS {
        guid Id PK
        guid BookingId FK "Restrict"
        string RecordingId
        string StoragePath
    }
```

> **Money:** *settlement* amounts (`Payment`, `Payout`) are stored as `long`
> **cents** to avoid floating-point rounding; *catalogue / price* amounts
> (`Booking.PriceUsd`, `Scholarship.FundingAmountUsd` / `ReviewFeeUsd`,
> `UserProfile.SessionFeeUsd`) are `decimal(p,2)` dollars. **`Payment` and `Payout` have zero DB-enforced FKs** —
> every edge they touch (payer, payee, related booking/application, payout) is a
> loose reference, so they are decoupled from the principals for audit retention.
> Settlement / config tables `STRIPE_WEBHOOK_EVENTS`, `PROFIT_SHARE_CONFIGS`,
> `FINANCIAL_CONFIG_RULES`, `COMPANY_REVIEW_PAYMENTS` are standalone (admin/Stripe
> driven) and omitted here for legibility — see the relational mapping.

---

## 7. Community & Chat

**Chen / Elmasri notation** (authentic book symbols):

![EERD Chen — Community & Chat](img/EERD_Community_Chat.svg)

**Crow's-foot detail companion** (every entity + key attributes):

![EERD — Community & Chat](img/eerd-05-community-chat.svg)

```mermaid
erDiagram
    FORUM_CATEGORIES ||--o{ FORUM_POSTS : "groups"
    USERS ||--o{ FORUM_POSTS : "authors"
    FORUM_POSTS ||--o{ FORUM_POSTS : "replies-to (self)"
    FORUM_POSTS ||--o{ FORUM_POST_ATTACHMENTS : "has"
    FORUM_POSTS ||--o{ FORUM_VOTES : "receives"
    FORUM_POSTS ||--o{ FORUM_FLAGS : "receives"
    FORUM_POSTS ||--o{ FORUM_BOOKMARKS : "saved-in"
    FORUM_POSTS ||--o{ FORUM_POST_TAGS : "tagged"
    FORUM_TAGS ||--o{ FORUM_POST_TAGS : "tags"
    USERS ||..o{ FORUM_VOTES : "casts (loose)"
    USERS ||..o{ CONVERSATIONS : "participates (loose)"
    CONVERSATIONS ||--o{ MESSAGES : "contains"
    USERS ||..o{ MESSAGES : "sends (loose)"
    USERS ||..o{ USER_BLOCKS : "blocks (loose, self)"

    FORUM_POSTS {
        guid Id PK
        guid AuthorId FK "Restrict"
        guid CategoryId FK "SetNull"
        guid ParentPostId FK "Restrict, null=root thread"
        string ModerationStatus "Visible|Hidden|Removed|PendingReview"
        int UpvoteCount "cached"
        int FlagCount "cached"
    }
    FORUM_CATEGORIES { guid Id PK
        string Slug UK }
    FORUM_POST_ATTACHMENTS { guid Id PK
        guid ForumPostId FK }
    FORUM_VOTES {
        guid Id PK
        guid ForumPostId FK
        guid UserId "loose (UK pair)"
        string VoteType "Up|Down"
    }
    FORUM_FLAGS {
        guid Id PK
        guid ForumPostId FK
        guid FlaggedByUserId "loose (UK pair)"
    }
    FORUM_BOOKMARKS {
        guid Id PK
        guid ForumPostId FK
        guid UserId "loose (UK pair)"
    }
    FORUM_TAGS { guid Id PK
        string Slug UK }
    FORUM_POST_TAGS {
        guid ForumPostId PK "FK -> ForumPosts"
        guid ForumTagId PK "FK -> ForumTags"
    }
    CONVERSATIONS {
        guid Id PK
        guid ParticipantOneId "loose (UK pair)"
        guid ParticipantTwoId "loose (UK pair)"
        datetimeoffset LastMessageAt
    }
    MESSAGES {
        guid Id PK
        guid ConversationId FK
        guid SenderId "loose ref"
        string Body
        bool IsDeleted
    }
    USER_BLOCKS {
        guid Id PK
        guid BlockerId "loose (UK pair)"
        guid BlockedUserId "loose (UK pair)"
    }
```

> `FORUM_POST_TAGS` is a **pure M:N junction** (composite PK = the two FKs) — a
> textbook many-to-many between posts and tags. Votes / flags / bookmarks each
> carry a **composite unique key** `(PostId, UserId)` enforcing *one per user per
> post*. `CONVERSATIONS` has a unique `(ParticipantOne, ParticipantTwo)` pair —
> one 1:1 DM channel per pair. **`FORUM_POST_ATTACHMENTS` is schema-only /
> planned** (verified): no upload or read path is wired yet — posts are
> markdown-only today.

---

## 8. Resources Hub & Notifications

**Chen / Elmasri notation** (authentic book symbols):

![EERD Chen — Resources & Notifications](img/EERD_Resources_Notifications.svg)

**Crow's-foot detail companion** (every entity + key attributes):

![EERD — Resources Hub & Notifications](img/eerd-06-resources-notifications.svg)

```mermaid
erDiagram
    USERS ||--o{ RESOURCES : "authors"
    RESOURCES ||--o{ RESOURCE_CHAPTERS : "split-into"
    RESOURCES ||--o{ RESOURCE_BOOKMARKS : "saved-in"
    RESOURCES ||--o{ RESOURCE_PROGRESS : "tracked-in"
    RESOURCE_PROGRESS ||--o{ RESOURCE_PROGRESS_CHILDREN : "per-chapter"
    RESOURCE_CHAPTERS ||..o{ RESOURCE_PROGRESS_CHILDREN : "for-chapter (loose)"
    USERS ||..o{ RESOURCE_BOOKMARKS : "saves (loose)"
    USERS ||..o{ RESOURCE_PROGRESS : "owns (loose)"
    USERS ||..o{ NOTIFICATIONS : "receives (loose)"
    USERS ||..o{ NOTIFICATION_PREFERENCES : "configures (loose)"

    RESOURCES {
        guid Id PK
        guid AuthorUserId FK
        string Slug UK
        string Type "Article|Guide|Checklist|VideoLink"
        string Status "Draft|PendingReview|Published|Hidden|Removed"
        string AuthorRole "denormalized"
    }
    RESOURCE_CHAPTERS {
        guid Id PK
        guid ResourceId FK
        int SortOrder
    }
    RESOURCE_BOOKMARKS {
        guid Id PK
        guid ResourceId FK
        guid UserId "loose (UK pair)"
    }
    RESOURCE_PROGRESS {
        guid Id PK
        guid ResourceId FK
        guid UserId "loose (UK pair)"
        int ChaptersCompletedCount
    }
    RESOURCE_PROGRESS_CHILDREN {
        guid Id PK
        guid ResourceProgressId FK
        guid ResourceChildId "loose (UK pair)"
        bool IsCompleted
    }
    NOTIFICATIONS {
        guid Id PK
        guid RecipientUserId "loose ref"
        string Type
        string Channel "InApp|Email|Push"
        bool IsRead
        string IdempotencyKey
    }
    NOTIFICATION_PREFERENCES {
        guid Id PK
        guid UserId "loose (UK triple)"
        string Type
        string Channel
        bool IsEnabled
    }
```

---

## 9. AI, Knowledge Base, Platform & Cross-cutting

**Chen / Elmasri notation** (authentic book symbols):

![EERD Chen — AI, Knowledge & Platform](img/EERD_AI_Knowledge_Platform.svg)

**Crow's-foot detail companion** (every entity + key attributes):

![EERD — AI, Knowledge, Platform & Cross-cutting](img/eerd-07-ai-knowledge-platform.svg)

```mermaid
erDiagram
    USERS ||--o{ RECOMMENDATION_CLICK_EVENTS : "clicks (student)"
    SCHOLARSHIPS ||--o{ RECOMMENDATION_CLICK_EVENTS : "clicked"
    AI_INTERACTIONS ||--o{ RECOMMENDATION_CLICK_EVENTS : "from (SetNull)"
    AI_INTERACTIONS ||--o| AI_REDACTION_AUDIT_SAMPLES : "sampled (1:1)"
    USERS ||--o{ AI_REDACTION_AUDIT_SAMPLES : "subject"
    USERS ||--o{ AI_REDACTION_AUDIT_SAMPLES : "reviewer"
    USERS ||--o| USER_RISK_FLAGS : "scored (1:1)"
    USERS ||..o{ AI_INTERACTIONS : "invokes (loose)"
    USERS ||..o{ AUDIT_LOGS : "actor (loose)"
    USERS ||..o{ USER_DATA_REQUESTS : "raises (loose)"
    USERS ||..o| SUCCESS_STORIES : "subject (loose)"

    AI_INTERACTIONS {
        guid Id PK
        guid UserId "loose ref"
        string Feature "Recommendation|Eligibility|Chatbot"
        string Provider "Stub|OpenAi|AzureOpenAi"
        decimal CostUsd
        int PromptTokens
        int CompletionTokens
    }
    RECOMMENDATION_CLICK_EVENTS {
        guid Id PK
        guid UserId FK "Restrict"
        guid ScholarshipId FK "Restrict"
        guid AiInteractionId FK "SetNull"
    }
    AI_REDACTION_AUDIT_SAMPLES {
        guid Id PK
        guid AiInteractionId FK "UK, 1:1 Restrict"
        guid UserId FK "Restrict"
        guid ReviewerUserId FK "Restrict"
        string Verdict "Clean|MissedEmail|MissedPhone|MissedCard"
    }
    KNOWLEDGE_DOCUMENTS {
        guid Id PK
        string SourceType "Scholarship|Faq|Resource|Consultant|CommunityPost"
        guid SourceId "polymorphic loose ref"
        string SourceKey "(SourceType,SourceKey) UK"
        bytes Embedding "packed float[]"
    }
    USER_RISK_FLAGS {
        guid Id PK
        guid UserId FK "UK, 1:1 Restrict"
        decimal Score "0..1"
        bool IsAtRisk
    }
    AUDIT_LOGS {
        guid Id PK
        guid ActorUserId "loose ref"
        string Action
        string TargetType "polymorphic"
        guid TargetId "polymorphic loose ref"
    }
    PLATFORM_SETTINGS {
        guid Id PK
        string Key UK
        string Value
        string ValueType "Text|Boolean|Number"
        string Category
    }
    USER_DATA_REQUESTS {
        guid Id PK
        guid UserId "loose ref"
        string Type "Export|Delete"
        string Status
    }
    SUCCESS_STORIES {
        guid Id PK
        guid StudentId "loose ref, nullable"
        bool IsApproved
        bool IsFeatured
    }
```

> `KNOWLEDGE_DOCUMENTS.SourceId` and `AUDIT_LOGS.(TargetType, TargetId)` are
> **polymorphic** references — they point at *any* entity type by id, so they
> cannot be a single FK. The vector `Embedding` is stored in-row as packed
> `varbinary`; cosine ranking runs in-process (a few hundred rows), so there is
> no separate vector-DB entity.

> **Cross-cutting entity status (verified against code).** `AI_INTERACTIONS`,
> `RECOMMENDATION_CLICK_EVENTS`, `AI_REDACTION_AUDIT_SAMPLES`, `USER_DATA_REQUESTS`
> (GDPR export/delete) and `AUDIT_LOGS` are **active** features (NFR / analytics-
> driven, not in a single SRS FR). `USER_RISK_FLAGS` is **active but read-only**
> from the API — rows are written out-of-band by the Power BI churn dataflow.
> **`SUCCESS_STORIES` is schema-only / planned** — seed data exists but there is
> no read/write code yet (homepage feature not built).

---

## 10. Cross-area relationships at a glance

| From | To | Ratio | Participation | Enforcement |
|---|---|---|---|---|
| User | UserProfile | 1 : 0..1 | profile total, user partial | **FK** Restrict |
| User ⋈ Role | (UserRoles) | M : N | — | **FK** (Identity); UserId Restrict, RoleId Cascade |
| Scholarship | Application | 1 : N | app side optional (external apps have no scholarship) | **FK** Restrict |
| Student (User) | Application | 1 : N | app total | **FK** Restrict |
| User (Student) ⋈ Scholarship | (SavedScholarships) | M : N | — | **loose** (unique index only) |
| Booking | Payment | 1 : 0..1 | both partial | **FK** SetNull |
| Booking | ConsultantReview | 1 : 0..1 | review total | **FK** Restrict, unique |
| Application | CompanyReview | 1 : 0..1 | review total | **FK** Restrict, unique |
| ForumPost ⋈ ForumTag | (ForumPostTag) | M : N | — | **FK** Cascade (both legs) |
| ForumPost | ForumPost (reply) | 1 : N | child partial (root has none) | **FK** Restrict (self) |
| User | Notification | 1 : N | — | **loose** |
| User | AuditLog (actor) | 1 : N | — | **loose** |
| AiInteraction | AiRedactionAuditSample | 1 : 0..1 | sample total | **FK** Restrict, unique |
| User | UserRiskFlag | 1 : 0..1 | flag total | **FK** Restrict, unique |

See **`02-RELATIONAL-MAPPING.md`** for the full table-by-table schema and
**`plantuml/`** for the Chen-notation (`@startchen`) sources.
