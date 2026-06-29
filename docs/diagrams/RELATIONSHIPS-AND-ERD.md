# ScholarPath — Class Relationships & ERD Description

A written companion to the diagrams in this folder. Part A describes every
**class relationship** (type, multiplicity, meaning, lifecycle). Part B is a
**full ERD description** (entities, relationships, cardinality, participation).

Reconciled against the EF model snapshot
`server/src/ScholarPath.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`.

---

## Notation key

**Class diagrams (UML)**

| Symbol | Name | Meaning |
|---|---|---|
| ▷ solid line | Generalization (inheritance) | "is-a"; child inherits the parent. |
| ┈▷ dashed line | Realization | class implements an interface. |
| ◆— filled diamond | Composition | strong ownership; the part **cannot exist without** the whole and is **cascade-deleted** with it. |
| ◇— hollow diamond | Aggregation | weak ownership; the part can exist independently. |
| — plain line | Association | a structural link (usually a foreign key). |
| `1`, `0..1`, `*` | Multiplicity | exactly one / optional one / many. |
| `/Name` | Derived member | computed, not stored. |
| `«enc»` | Encrypted | column encrypted at rest. |

**ERD (Chen notation)** — rectangle = entity; double rectangle = weak entity;
diamond = relationship; double diamond = identifying relationship; ellipse =
attribute; underlined = key; double ellipse = multivalued; dashed ellipse =
derived. Edge labels `1` / `N` / `M` give cardinality; a double line = total
participation, single line = partial.

---

# Part A — Class Relationships

## A.0 Base types (Fig 132)

| Relationship | Type | Meaning |
|---|---|---|
| `BaseEntity` ◁ `AuditableEntity` | Generalization | Every auditable domain entity is a `BaseEntity` (gets `Id` + domain-event behaviour) and adds the audit triple (`CreatedAt/By`, `UpdatedAt/By`, `RowVersion`). |
| `BaseEntity` ┈▷ `DomainEvent` | Dependency | A `BaseEntity` *raises* domain events; it holds a read-only collection and exposes `RaiseDomainEvent` / `ClearDomainEvents`. |
| `IdentityUser<Guid>` ◁ `ApplicationUser` | Generalization | `ApplicationUser` extends ASP.NET Identity's user. |
| `ApplicationUser` ┈▷ `ISoftDeletable` | Realization | The user is soft-deletable (`IsDeleted`, `DeletedAt`, `DeletedByUserId`). |

> `ApplicationUser` is the **one exception** that does not inherit `AuditableEntity`; it carries a stripped audit triple only.

## A.1 Identity & Profile (Fig 133)

| Source | Rel. | Target | Mult. | Type | Description |
|---|---|---|---|---|---|
| ApplicationUser | roles | ApplicationRole | `*`—`*` | Association (M:N) | A user holds many roles (Student/Company/Consultant/Admin/Unassigned) and a role is held by many users. Resolved by the `UserRoles` join table. `ActiveRole` names the currently-selected one. |
| ApplicationUser | profile | UserProfile | `1`—`0..1` | Association | A user optionally has exactly one profile (created during onboarding). 1:1. |
| UserProfile | education | EducationEntry | `1`◆—`*` | **Composition** | Education rows belong to the profile and are deleted with it. |
| UserProfile | expertise | ExpertiseTag | `*`—`*` | Association (M:N) | A consultant profile is tagged with many expertise tags; each tag classifies many profiles. *(Implementation denormalizes the selection to `ExpertiseTagsJson`; the conceptual relationship is M:N.)* |
| ApplicationUser | issues | RefreshToken | `1`—`*` | Association | One user, many refresh tokens (token-rotation chain). |
| ApplicationUser | resets | PasswordResetToken | `1`—`*` | Association | One user, many password-reset tokens over time. |
| ApplicationUser | attempts | LoginAttempt | `1`—`*` | Association | Append-only audit of login attempts (drives lockout). |
| ApplicationUser | requests | UpgradeRequest | `1`—`*` | Association | A user may file many role-upgrade requests. |
| UpgradeRequest | files | UpgradeRequestFile | `1`◆—`*` | **Composition** | Supporting documents owned by the request. |
| UpgradeRequest | links | UpgradeRequestLink | `1`◆—`*` | **Composition** | Supporting links owned by the request. |

## A.2 Scholarships, Applications & Documents (Fig 134)

| Source | Rel. | Target | Mult. | Type | Description |
|---|---|---|---|---|---|
| Category | classifies | Scholarship | `0..1`—`*` | Association | A scholarship optionally belongs to one category; a category groups many scholarships. |
| Scholarship | details | ScholarshipChild | `1`◆—`*` | **Composition** | Flexible key/value detail rows owned by the scholarship. |
| Scholarship | bookmarks | SavedScholarship | `1`—`*` | Association | A scholarship can be saved by many users (`SavedScholarship` is the per-user bookmark; also links to the saving user). |
| Scholarship | receives | ApplicationTracker | `1`—`0..1` | Association | A given student has at most one active application tracker per scholarship. |
| Scholarship | under | CompanyReviewRequest | `1`—`*` | Association | A scholarship can be the subject of many paid company-review requests. |
| ApplicationTracker | history | ApplicationTrackerChild | `1`◆—`*` | **Composition** | Status-history / note rows owned by the tracker. |
| ApplicationTracker | supportedBy | Document | `1`—`*` | Association | **(fixed)** One application is supported by **many** documents. |
| ApplicationTracker | ratedBy | CompanyReview | `1`—`0..1` | Association | An application may receive at most one company review. |

> `CompanyReviewPayment` is **legacy/deprecated** and stands alone; the active paid-review flow settles through `Payment` (see A.3).

## A.3 Consultant Booking, Payments & Ratings (Fig 135)

| Source | Rel. | Target | Mult. | Type | Description |
|---|---|---|---|---|---|
| ConsultantAvailability | reserves | ConsultantBooking | `0..1`—`*` | Association | A booking optionally reserves an availability slot; a slot can back many bookings over time. |
| ConsultantBooking | settledBy | Payment | `1`—`0..1` | Association | A booking is settled by at most one payment (hold → capture). |
| ConsultantBooking | ratedBy | ConsultantReview | `1`—`0..1` | Association | A completed booking may get one consultant review. |
| ConsultantBooking | recordedAs | SessionRecording | `1`◆—`*` | **Composition** | Recording segments owned by the booking. |
| Payout | aggregates | Payment | `1`◇—`*` | **Aggregation** | A payout bundles many captured payments for settlement to a consultant; payments exist independently of the payout (hence aggregation, not composition). |
| CompanyReviewRequest | settledBy | Payment | `1`—`0..1` | Association | **(fixed)** The paid company-review path also settles through `Payment`. |

> **`Payment` is polymorphic:** it settles **either** a `ConsultantBooking` (`RelatedBookingId`) **or** a `CompanyReviewRequest` (`RelatedApplicationId`), and carries `PayerUserId` / `PayeeUserId`. `StripeWebhookEvent`, `ProfitShareConfig`, `FinancialConfigRule` are standalone config/event entities (no associations).

## A.4 Community & Chat (Fig 136)

| Source | Rel. | Target | Mult. | Type | Description |
|---|---|---|---|---|---|
| ForumCategory | groups | ForumPost | `0..1`—`*` | Association | A post optionally sits in one category. |
| ForumPost | parent (reply) | ForumPost | `0..1`—`*` | **Self-association** | A post may be a reply to one parent post; a post has many replies. |
| ForumPost | attaches | ForumPostAttachment | `1`◆—`*` | **Composition** | Attachments owned by the post. |
| ForumPost | votes | ForumVote | `1`◆—`*` | **Composition** | Votes owned by the post (each also references the voting user). |
| ForumPost | flags | ForumFlag | `1`◆—`*` | **Composition** | Moderation flags owned by the post. |
| ForumPost | bookmarks | ForumBookmark | `1`◆—`*` | **Composition** | Per-user bookmarks of the post. |
| ForumPost | tagged via ForumPostTag | ForumTag | `*`—`*` | Association (M:N) | Posts and tags are many-to-many; `ForumPostTag` is the **association/junction class** (composite PK). |
| ChatConversation | messages | ChatMessage | `1`◆—`*` | **Composition** | Messages owned by the conversation. |

> `UserBlock` carries **two** user references — `BlockerId` and `BlockedUserId` — i.e. two distinct associations to `ApplicationUser`. A conversation likewise references two participants.

## A.5 Resources Hub & Notifications (Fig 137)

| Source | Rel. | Target | Mult. | Type | Description |
|---|---|---|---|---|---|
| Resource | chapters | ResourceChild | `1`◆—`*` | **Composition** | Chapters owned by the resource. |
| Resource | savedBy | ResourceBookmark | `1`—`*` | Association | Per-user bookmarks of the resource. |
| Resource | trackedIn | ResourceProgress | `1`—`*` | Association | One progress record per (user, resource). |
| ResourceProgress | perChapter | ResourceProgressChild | `1`◆—`*` | **Composition** | Per-chapter completion rows owned by the progress record. |

> `ResourceProgress./ChaptersCompletedCount` is **derived** from the child rows. `Notification` and `NotificationPreference` associate to the user (other fragment).

## A.6 AI, Knowledge & Platform (Fig 138)

| Source | Rel. | Target | Mult. | Type | Description |
|---|---|---|---|---|---|
| AiInteraction | aiAttributed | RecommendationClickEvent | `1`—`*` | Association | Clicks attributed to one AI interaction. |
| AiInteraction | sampled | AiRedactionAuditSample | `1`—`0..1` | Association | An interaction may be sampled once for redaction audit. |
| RecommendationClickEvent | clicked | Scholarship | `*`—`1` | Association | Each click references the scholarship that was clicked. |

> `KnowledgeDocument`, `AuditLog`, `UserDataRequest`, `SuccessStory`, `UserRiskFlag`, `PlatformSetting` are cross-cutting; most associate only to the user. `AuditLog.TargetId` is a **polymorphic** reference (no DB-level FK).

---

# Part B — ERD (full description)

## B.0 Specialization — User hierarchy (Fig 141)

`USER` is **specialized** into four subclasses:

- **Type:** overlapping (circle **o**) — a user may simultaneously be more than one role (e.g. Student **and** Consultant).
- **Participation:** partial (single line) — a freshly-registered *Unassigned* user belongs to no subclass yet.
- **Constraint:** subset (⊆) — every subclass **IS-A** `USER`.

| Subclass | Distinguishing attributes |
|---|---|
| STUDENT | AcademicLevel, Gpa, FieldOfStudy, **PreferredCountries** (multivalued) |
| COMPANY | OrganizationLegalName, VerificationStatus, CompanyType, *CompanyAverageRating* (derived) |
| CONSULTANT | SessionFeeUsd, **ExpertiseTags** (multivalued), **Languages** (multivalued), StripeConnectStatus |
| ADMIN | (no role-specific attributes) |

**Mapping:** this specialization is implemented with a **single-table strategy** — `Users` + one shared `UserProfiles` table; columns not relevant to a role are NULL.

## B.1 Identity & Profile (Fig 142)

| Entity 1 | Card. | Relationship | Card. | Entity 2 | Participation / notes |
|---|---|---|---|---|---|
| USER | 1 | HAS_PROFILE | 1 | USER_PROFILE | 1:1; total on the profile side. |
| USER | M | HAS_ROLE | N | ROLE | M:N. |
| USER_PROFILE | 1 | HAS_EDU | N | EDUCATION_ENTRY *(weak)* | Identifying; entry depends on the profile. |
| USER | 1 | ISSUES | N | REFRESH_TOKEN | 1:N. |
| USER | 1 | RESETS | N | PASSWORD_RESET_TOKEN | 1:N. |
| USER | 1 | ATTEMPTS | N | LOGIN_ATTEMPT | 1:N. |
| USER | 1 | REQUESTS_UPG | N | UPGRADE_REQUEST | 1:N. |
| UPGRADE_REQUEST | 1 | HAS_FILE | N | UPGRADE_REQUEST_FILE *(weak)* | Identifying. |
| UPGRADE_REQUEST | 1 | HAS_LINK | N | UPGRADE_REQUEST_LINK *(weak)* | Identifying. |
| USER_PROFILE | M | HAS_EXPERTISE | N | EXPERTISE_TAG | **(fixed)** M:N; tag is no longer floating. |

Derived: `ProfileCompletenessPercent`, `CompanyAverageRating`. Multivalued: `PreferredCountries`, `ExpertiseTags`.

## B.2 Scholarships & Applications (Fig 143)

| Entity 1 | Card. | Relationship | Card. | Entity 2 | Notes |
|---|---|---|---|---|---|
| CATEGORY | 1 | CLASSIFIES | N | SCHOLARSHIP | optional category. |
| SCHOLARSHIP | 1 | HAS_DETAIL | N | SCHOLARSHIP_CHILD *(weak)* | identifying. |
| USER | 1 | SAVES | N | SAVED_SCHOLARSHIP | bookmark; also → SCHOLARSHIP. |
| USER | 1 | OWNS | N | APPLICATION | student owns applications. |
| SCHOLARSHIP | 1 | RECEIVES | N | APPLICATION | per scholarship. |
| APPLICATION | 1 | HAS_HISTORY | N | APPLICATION_CHILD *(weak)* | identifying. |
| APPLICATION | 1 | SUPPORTS | N | DOCUMENT | **(fixed)** one application → many documents. |
| APPLICATION | 1 | RATED_BY | 1 | COMPANY_REVIEW | optional review. |
| SCHOLARSHIP | 1 | UNDER | N | COMPANY_REVIEW_REQUEST | paid review request. |
| USER | 1 | RAISES | N | COMPANY_REVIEW_REQUEST | requester. |
| APPLICATION | 1 | LEGACY_PAY | 1 | COMPANY_REVIEW_PAYMENT *(legacy)* | retained for history. |

## B.3 Booking, Payments & Ratings (Fig 144)

| Entity 1 | Card. | Relationship | Card. | Entity 2 | Notes |
|---|---|---|---|---|---|
| USER | 1 | PUBLISHES | N | AVAILABILITY | consultant publishes slots. |
| USER | 1 | BOOKS | N | BOOKING | student side. |
| USER | 1 | CONDUCTS | N | BOOKING | consultant side (two roles, both `USER`). |
| AVAILABILITY | 1 | RESERVES | N | BOOKING | slot backing. |
| BOOKING | 1 | SETTLED_BY | 1 | PAYMENT | hold/capture. |
| BOOKING | 1 | RATED_BY | 1 | CONSULTANT_REVIEW | optional. |
| BOOKING | 1 | RECORDED_AS | N | SESSION_RECORDING *(weak)* | identifying. |
| PAYOUT | 1 | AGGREGATES | N | PAYMENT | bundles captured payments. |
| COMPANY_REVIEW_REQUEST | 1 | SETTLES | 1 | PAYMENT | **(fixed)** paid-review settlement. |
| USER | 1 | PAYER / PAYEE | N | PAYMENT | payer/payee references. |

Standalone (no FK): STRIPE_WEBHOOK_EVENT, PROFIT_SHARE_CONFIG, FINANCIAL_CONFIG_RULE.

## B.4 Community & Chat (Fig 145)

| Entity 1 | Card. | Relationship | Card. | Entity 2 | Notes |
|---|---|---|---|---|---|
| FORUM_CATEGORY | 1 | GROUPS | N | FORUM_POST | optional category. |
| USER | 1 | AUTHORS | N | FORUM_POST | author. |
| FORUM_POST | 1 | REPLIES | N | FORUM_POST | self / threading. |
| FORUM_POST | 1 | ATTACHES | N | FORUM_POST_ATTACHMENT *(weak)* | identifying. |
| USER + FORUM_POST | 1+1 | CASTS / RECEIVES_VOTE | N | FORUM_VOTE | **(fixed)** who voted + on what. |
| USER + FORUM_POST | 1+1 | FILES / RECEIVES_FLAG | N | FORUM_FLAG | **(fixed)** who flagged + what. |
| USER + FORUM_POST | 1+1 | SAVES / SAVED_IN | N | FORUM_BOOKMARK | **(fixed)** who bookmarked + what. |
| FORUM_POST ↔ FORUM_TAG | M:N | TAGGED / TAGS via FORUM_POST_TAG *(weak)* | | | junction. |
| USER | N | PARTICIPATES | M | CONVERSATION | via two participant FKs. |
| CONVERSATION | 1 | CONTAINS | N | MESSAGE *(weak)* | identifying. |
| USER | 1 | SENDS | N | MESSAGE | **(fixed)** sender. |
| USER | 1 (×2) | BLOCKER / BLOCKED | N | USER_BLOCK | **(fixed)** two distinct roles. |

## B.5 Resources & Notifications (Fig 146)

| Entity 1 | Card. | Relationship | Card. | Entity 2 | Notes |
|---|---|---|---|---|---|
| USER | 1 | AUTHORS | N | RESOURCE | admin author. |
| RESOURCE | 1 | SPLIT_INTO | N | RESOURCE_CHAPTER *(weak)* | identifying. |
| USER + RESOURCE | 1+1 | SAVES / SAVED_IN | N | RESOURCE_BOOKMARK | **(fixed)** owning user. |
| USER + RESOURCE | 1+1 | TRACKS / TRACKED_IN | N | RESOURCE_PROGRESS | **(fixed)** owning user. |
| RESOURCE_PROGRESS | 1 | PER_CHAPTER | N | RESOURCE_PROGRESS_CHILD *(weak)* | identifying. |
| USER | 1 | RECEIVES | N | NOTIFICATION | recipient. |
| USER | 1 | CONFIGURES | N | NOTIFICATION_PREFERENCE | per-channel prefs. |

Derived: `ResourceProgress.ChaptersCompletedCount`.

## B.6 AI, Knowledge & Platform (Fig 147)

| Entity 1 | Card. | Relationship | Card. | Entity 2 | Notes |
|---|---|---|---|---|---|
| USER | 1 | INVOKES | N | AI_INTERACTION | usage + cost. |
| AI_INTERACTION | 1 | FROM_AI | N | RECOMMENDATION_CLICK_EVENT | attribution. |
| SCHOLARSHIP | 1 | CLICKED | N | RECOMMENDATION_CLICK_EVENT | clicked target. |
| AI_INTERACTION | 1 | SAMPLED | 1 | AI_REDACTION_AUDIT_SAMPLE | optional sample. |
| USER | 1 | SCORED | N | USER_RISK_FLAG | risk scoring. |
| USER | 1 | ACTOR_OF | N | AUDIT_LOG | actor; `TargetId` polymorphic. |
| USER | 1 | RAISES | N | USER_DATA_REQUEST | GDPR request. |
| USER | 1 | STARS_IN | N | SUCCESS_STORY | featured student. |

Standalone (no FK): KNOWLEDGE_DOCUMENT, PLATFORM_SETTING.

---

## Cardinality summary (quick reference)

- **1:1** — User↔UserProfile; Booking↔Payment; Booking↔ConsultantReview; Application↔CompanyReview; AiInteraction↔AiRedactionAuditSample.
- **1:N** — almost every owner→child (composition) and User→owned-records relationship.
- **M:N** — User↔Role; UserProfile↔ExpertiseTag; ForumPost↔ForumTag (via junctions).
- **Self** — ForumPost→ForumPost (reply threading).
- **Two-role to same entity** — UserBlock (Blocker/Blocked); Conversation (ParticipantOne/Two); Booking (Student/Consultant); Payment (Payer/Payee).
- **Polymorphic** — Payment (booking *or* application); AuditLog.TargetId.
