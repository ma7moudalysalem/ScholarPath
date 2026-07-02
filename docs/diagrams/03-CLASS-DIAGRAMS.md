# ScholarPath — Class Diagrams (UML / Sommerville)

> UML class diagrams in the style of *Sommerville, Software Engineering (9th ed.)*:
> classes (name / attributes / operations), **generalization** (hollow triangle
> `<|--`), **realization** of interfaces (`<|..`), **composition** (filled
> diamond `*--`), **aggregation** (hollow diamond `o--`), plain **association**
> with role names and multiplicities. Diagrams below are Mermaid `classDiagram`
> (render on GitHub); the embedded PNGs are pre-rendered; PlantUML equivalents
> are in `plantuml/`.
>
> Grounded in `server/src/ScholarPath.Domain/Entities/*` and the application
> interfaces in `server/src/ScholarPath.Application/Common/Interfaces/*` with
> their Infrastructure implementations.

## Legend

| UML element | Mermaid | Meaning |
|---|---|---|
| Generalization | `Base <|-- Derived` | inheritance (`AuditableEntity` ← entities) |
| Realization | `Interface <|.. Class` | a class implements an interface |
| Composition | `Whole *-- Part` | part cannot exist without the whole |
| Aggregation | `Whole o-- Part` | part can exist independently |
| Association | `A "1" --> "*" B : role` | reference with multiplicity |
| `<<abstract>>` / `<<interface>>` | stereotype | abstract base / port |

---

> **Authentic UML class diagrams (PlantUML, vector SVG — sharp at any zoom):**
> [domain model](img/ScholarPath_Domain_Model.svg) ·
> [ports & adapters](img/ScholarPath_Ports_Adapters.svg) — source
> [`plantuml/class-diagrams.puml`](plantuml/class-diagrams.puml). The Mermaid
> views below are equivalent UML and are now embedded as vector SVG too.

## 1. Domain foundation + Identity, Profile, Scholarships & Applications

![Class diagram — core domain](img/class-01-core-domain.svg)

```mermaid
classDiagram
    direction LR
    class BaseEntity {
        <<abstract>>
        +Guid Id
        +IReadOnlyCollection~DomainEvent~ DomainEvents
        +RaiseDomainEvent(e) void
        +ClearDomainEvents() void
    }
    class AuditableEntity {
        <<abstract>>
        +DateTimeOffset CreatedAt
        +Guid? CreatedByUserId
        +DateTimeOffset? UpdatedAt
        +Guid? UpdatedByUserId
        +byte[]? RowVersion
    }
    class IdentityUser~Guid~ {
        <<ASP.NET Identity>>
    }
    class IdentityRole~Guid~ {
        <<ASP.NET Identity>>
    }
    class ISoftDeletable {
        <<interface>>
        +bool IsDeleted
        +DateTimeOffset DeletedAt
        +Guid DeletedByUserId
    }
    class ApplicationUser {
        +string Email
        +string FirstName
        +string LastName
        +AccountStatus AccountStatus
        +string? ActiveRole
        +bool IsOnboardingComplete
        +FullName() string
    }
    class ApplicationRole { +string Name +string Description }
    class UserProfile {
        +decimal? Gpa
        +decimal? SessionFeeUsd
        +decimal? CompanyAverageRating
        +StripeConnectStatus StripeConnectStatus
        +int? ProfileCompletenessPercent
    }
    class EducationEntry {
        +string InstitutionName
        +string Degree
        +string FieldOfStudy
    }
    class Scholarship {
        +string Slug
        +ScholarshipStatus Status
        +ListingMode Mode
        +DateTimeOffset Deadline
        +decimal? ReviewFeeUsd
    }
    class ApplicationTracker {
        +ApplicationStatus Status
        +ApplicationMode Mode
        +DateTimeOffset SubmittedAt
        +IsActive() bool
        +IsReadOnly() bool
    }
    class Document {
        +DocumentCategory Category
        +string StoragePath
        +long SizeBytes
    }
    class CompanyReviewRequest {
        +CompanyReviewRequestStatus Status
        +decimal ReviewFeeUsdSnapshot
    }
    class CompanyReview {
        +int Rating
        +bool IsHiddenByAdmin
    }

    IdentityUser~Guid~ <|-- ApplicationUser
    IdentityRole~Guid~ <|-- ApplicationRole
    BaseEntity <|-- AuditableEntity
    AuditableEntity <|-- UserProfile
    AuditableEntity <|-- EducationEntry
    AuditableEntity <|-- Scholarship
    AuditableEntity <|-- ApplicationTracker
    AuditableEntity <|-- Document
    AuditableEntity <|-- CompanyReviewRequest
    AuditableEntity <|-- CompanyReview
    ISoftDeletable <|.. ApplicationUser
    ISoftDeletable <|.. Scholarship
    ISoftDeletable <|.. ApplicationTracker

    ApplicationUser "1" --> "0..1" UserProfile : profile
    UserProfile "1" *-- "*" EducationEntry : education
    ApplicationUser "0..1" o-- "*" Scholarship : owns
    Scholarship "0..1" --> "*" ApplicationTracker : receives
    ApplicationUser "1" --> "*" ApplicationTracker : submits(student)
    ApplicationUser "1" --> "*" Document : owns
    ApplicationTracker "0..1" --> "*" Document : supportedBy
    ApplicationTracker "1" --> "0..1" CompanyReview : ratedBy
    Scholarship "1" --> "*" CompanyReviewRequest : under
    ApplicationUser "1" --> "*" CompanyReviewRequest : raises(student)
```

> **`ApplicationUser` / `ApplicationRole`** extend ASP.NET Identity's
> `IdentityUser<Guid>` / `IdentityRole<Guid>` — **not** `BaseEntity`/`AuditableEntity`.
> Because `ApplicationUser` cannot inherit `BaseEntity`, it re-implements the
> domain-event plumbing manually. **`ISoftDeletable` is realized by far more
> entities than the three shown** (also `Document`, `CompanyReview(Request)`,
> `ConsultantReview`, `ConsultantAvailability`, `ConsultantBooking`, `Payment`,
> `ChatMessage`, `ForumPost`, `Resource`, `Notification`, `SessionRecording`,
> `SuccessStory`); only a representative subset is drawn to keep the diagram legible.

---

## 2. Consultant Booking, Payments, Ratings & Recording

![Class diagram — booking, payments, ratings](img/class-02-booking-payments.svg)

```mermaid
classDiagram
    direction LR
    class ConsultantAvailability {
        +bool IsRecurring
        +bool IsActive
        +DayOfWeek DayOfWeek
        +TimeOnly StartTime
    }
    class ConsultantBooking {
        +DateTimeOffset ScheduledStartAt
        +int DurationMinutes
        +decimal PriceUsd
        +BookingStatus Status
        +bool IsNoShowStudent
    }
    class Payment {
        +PaymentType Type
        +PaymentStatus Status
        +long AmountCents
        +long ProfitShareAmountCents
        +long PayeeAmountCents
        +string IdempotencyKey
    }
    class Payout {
        +long AmountCents
        +PayoutStatus Status
        +string IncludedPaymentIdsJson
    }
    class ConsultantReview {
        +int Rating
        +string Comment
        +bool IsHiddenByAdmin
    }
    class SessionRecording {
        +string RecordingId
        +string StoragePath
        +long SizeBytes
    }
    class ApplicationUser {
        <<from §1>>
    }

    ApplicationUser "1" o-- "*" ConsultantAvailability : publishes(consultant)
    ApplicationUser "1" --> "*" ConsultantBooking : books(student)
    ApplicationUser "1" --> "*" ConsultantBooking : conducts(consultant)
    ConsultantAvailability "0..1" --> "*" ConsultantBooking : reserves
    ConsultantBooking "1" --> "0..1" Payment : settledBy
    ConsultantBooking "1" --> "0..1" ConsultantReview : ratedBy
    ConsultantBooking "1" *-- "*" SessionRecording : recordedAs
    Payout "1" o-- "*" Payment : aggregates
    ApplicationUser "1" --> "*" ConsultantReview : authors(student)
    ApplicationUser "1" --> "*" ConsultantReview : rated(consultant)
```

> `Payment`/`Payout` associations are drawn as UML associations but are
> **application-enforced** (no DB FK) — see the relational mapping. `Booking *--
> SessionRecording` is composition (a recording has no life outside its booking).

---

## 3. Community, Chat, Resources, Notifications & AI

![Class diagram — community, resources, AI](img/class-03-community-resources-ai.svg)

```mermaid
classDiagram
    direction LR
    class ForumPost {
        +string Title
        +string BodyMarkdown
        +PostModerationStatus ModerationStatus
        +int UpvoteCount
        +int FlagCount
    }
    class ForumVote { +VoteType VoteType }
    class ForumFlag { +string Reason +bool IsValid }
    class ForumTag { +string Name +string Slug }
    class ForumPostTag { <<junction>> }
    class ChatConversation { +DateTimeOffset LastMessageAt }
    class ChatMessage { +string Body +DateTimeOffset SentAt +DateTimeOffset ReadAt }
    class Resource {
        +string Slug
        +ResourceType Type
        +ResourceStatus Status
        +string AuthorRole
    }
    class ResourceChild { +string TitleEn +int SortOrder }
    class ResourceProgress { +int ChaptersCompletedCount }
    class Notification {
        +NotificationType Type
        +NotificationChannel Channel
        +bool IsRead
    }
    class AiInteraction {
        +AiFeature Feature
        +AiProvider Provider
        +decimal CostUsd
        +int PromptTokens
    }
    class KnowledgeDocument {
        +KnowledgeSourceType SourceType
        +byte[] Embedding
        +string ContentHash
    }

    ForumPost "1" --> "0..1" ForumPost : parent(reply)
    ForumPost "1" *-- "*" ForumVote : votes
    ForumPost "1" *-- "*" ForumFlag : flags
    ForumPost "*" --> "*" ForumTag : tags
    ForumPostTag ..> ForumPost
    ForumPostTag ..> ForumTag
    ChatConversation "1" *-- "*" ChatMessage : messages
    Resource "1" *-- "*" ResourceChild : chapters
    Resource "1" --> "*" ResourceProgress : trackedBy
    AiInteraction ..> KnowledgeDocument : cites (RAG, via MetadataJson — no FK)
```

---

## 4. Application architecture — ports & adapters (Clean Architecture)

![Class diagram — ports and adapters](img/class-04-ports-adapters.svg)

The Application layer defines **ports** (interfaces); the Infrastructure layer
provides **adapters** (implementations), selected at start-up by DI / config
(`Ai__Provider`, environment). This is why the platform can swap a dev stub for
Azure OpenAI without touching application code.

```mermaid
classDiagram
    direction LR
    class IApplicationDbContext
    class IAiService {
        +Recommend()
        +CheckEligibility()
        +Ask()
    }
    class IEmbeddingService {
        +Embed(text)
    }
    class IStripeService {
        +CreatePaymentIntent()
        +Capture()
        +Refund()
    }
    class IMeetingService {
        +CreateRoom()
        +StartRecording()
    }
    class IEmailService {
        +SendAsync(msg)
    }
    class INotificationDispatcher {
        +DispatchAsync()
    }
    class IFileScanService {
        +ScanAsync()
    }
    class IFieldEncryptionService {
        +Encrypt()
        +Decrypt()
    }
    class IPowerBiService {
        +GetEmbedToken()
    }
    class IEventPublisher {
        +PublishAsync()
    }

    class LocalAiService
    class LocalEmbeddingService
    class StubMeetingService
    class ApplicationDbContext
    class AzureOpenAiService
    class OpenAiService
    class AzureOpenAiEmbeddingService
    class OpenAiEmbeddingService
    class StripeService
    class AzureCommunicationMeetingService
    class MailKitEmailService
    class NotificationDispatcher
    class ClamAvFileScanService
    class AesGcmFieldEncryptionService
    class PowerBiService
    class EventHubPublisher

    IApplicationDbContext <|.. ApplicationDbContext
    IAiService <|.. AzureOpenAiService
    IAiService <|.. OpenAiService
    IAiService <|.. LocalAiService
    IEmbeddingService <|.. AzureOpenAiEmbeddingService
    IEmbeddingService <|.. OpenAiEmbeddingService
    IEmbeddingService <|.. LocalEmbeddingService
    IStripeService <|.. StripeService
    IMeetingService <|.. AzureCommunicationMeetingService
    IMeetingService <|.. StubMeetingService
    IEmailService <|.. MailKitEmailService
    INotificationDispatcher <|.. NotificationDispatcher
    IFileScanService <|.. ClamAvFileScanService
    IFieldEncryptionService <|.. AesGcmFieldEncryptionService
    IPowerBiService <|.. PowerBiService
    IEventPublisher <|.. EventHubPublisher
```

> **Production adapters** (from config): `AzureOpenAiService` +
> `AzureOpenAiEmbeddingService` (`Ai__Provider=AzureOpenAi`),
> `StripeService`, `AzureCommunicationMeetingService` (video),
> `MailKitEmailService` (SMTP), `NotificationDispatcher` (writes
> `Notifications` + e-mail + SignalR), `ClamAvFileScanService`,
> `AesGcmFieldEncryptionService` (with a Key Vault key provider),
> `PowerBiService` + `EventHubPublisher` (analytics). **Every port has a
> config-selected dev fall-back** — `LocalAiService`, `LocalEmbeddingService`,
> `StubMeetingService`, `StubEmailService`, `StubStripeService`,
> `NoOpFileScanService`, `StubPowerBiService`, `StubEventPublisher` — but only the
> AI / embedding / meeting stubs are drawn above to keep the diagram legible.
> A third AI provider, `OpenAiService` + `OpenAiEmbeddingService`, is selected by
> `Ai__Provider=OpenAi` (OpenAI-direct, not Azure).
