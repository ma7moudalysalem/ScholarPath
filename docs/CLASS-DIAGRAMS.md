# Class Diagrams (UML)

Six class diagrams covering the key architectural groupings. Rendered natively by GitHub via Mermaid `classDiagram`.

## 1. Clean Architecture — project references

```mermaid
classDiagram
  class ScholarPath_Domain {
    <<class library>>
    Entities (40)
    Enums
    DomainEvent records
    Interfaces (ICurrentUserService, IDateTimeService)
  }
  class ScholarPath_Application {
    <<class library>>
    Commands + Queries (MediatR)
    Validators (FluentValidation)
    DTOs + Mappings
    Pipeline Behaviors
    Application interfaces
    DependencyInjection
  }
  class ScholarPath_Infrastructure {
    <<class library>>
    ApplicationDbContext
    EntityConfigurations
    TokenService, StubServices
    Hubs (Chat, Notification, Community)
    Jobs (Hangfire)
    Webhooks (Stripe)
    DependencyInjection
  }
  class ScholarPath_API {
    <<ASP.NET Core 10>>
    Program.cs
    Controllers
    Middleware
  }

  ScholarPath_Application --> ScholarPath_Domain : references
  ScholarPath_Infrastructure --> ScholarPath_Application : implements interfaces
  ScholarPath_Infrastructure --> ScholarPath_Domain : references
  ScholarPath_API --> ScholarPath_Application : wires
  ScholarPath_API --> ScholarPath_Infrastructure : DI registration
```

## 2. Domain aggregates (sample — Booking context)

```mermaid
classDiagram
  class AuditableEntity {
    <<abstract>>
    +Guid Id
    +DateTimeOffset CreatedAt
    +Guid? CreatedByUserId
    +DateTimeOffset? UpdatedAt
    +Guid? UpdatedByUserId
    +byte[]? RowVersion
  }
  class ISoftDeletable {
    <<interface>>
    +bool IsDeleted
    +DateTimeOffset? DeletedAt
    +Guid? DeletedByUserId
  }
  class ApplicationUser {
    +string FirstName
    +string LastName
    +AccountStatus AccountStatus
    +bool IsOnboardingComplete
    +string? ActiveRole
    +string FullName
    +RaiseDomainEvent(DomainEvent)
  }
  class ConsultantAvailability {
    +Guid ConsultantId
    +DayOfWeek? DayOfWeek
    +TimeOnly? StartTime
    +TimeOnly? EndTime
    +DateTimeOffset? SpecificStartAt
    +DateTimeOffset? SpecificEndAt
    +string Timezone
    +bool IsRecurring
    +bool IsActive
  }
  class ConsultantBooking {
    +Guid StudentId
    +Guid ConsultantId
    +Guid? AvailabilityId
    +DateTimeOffset ScheduledStartAt
    +DateTimeOffset ScheduledEndAt
    +int DurationMinutes
    +decimal PriceUsd
    +BookingStatus Status
    +string? MeetingUrl
    +Guid? PaymentId
    +bool IsNoShowStudent
    +bool IsNoShowConsultant
  }
  class ConsultantReview {
    +Guid BookingId
    +Guid StudentId
    +Guid ConsultantId
    +int Rating
    +string? Comment
    +bool IsHiddenByAdmin
  }

  AuditableEntity <|-- ApplicationUser
  AuditableEntity <|-- ConsultantAvailability
  AuditableEntity <|-- ConsultantBooking
  AuditableEntity <|-- ConsultantReview
  ISoftDeletable <|.. ApplicationUser
  ISoftDeletable <|.. ConsultantAvailability
  ISoftDeletable <|.. ConsultantBooking
  ISoftDeletable <|.. ConsultantReview
  ApplicationUser "1" <-- "*" ConsultantAvailability : Consultant
  ApplicationUser "1" <-- "*" ConsultantBooking : Student, Consultant
  ConsultantBooking "1" -- "0..1" ConsultantReview
  ConsultantAvailability "1" o-- "*" ConsultantBooking
```

## 3. CQRS vertical slice (Register example)

```mermaid
classDiagram
  class IRequest~TResponse~ {
    <<interface>>
  }
  class IRequestHandler~TRequest, TResponse~ {
    <<interface>>
    +Handle(TRequest, CancellationToken) Task~TResponse~
  }
  class IPipelineBehavior~TRequest, TResponse~ {
    <<interface>>
    +Handle(TRequest, next, CancellationToken) Task~TResponse~
  }
  class RegisterCommand {
    +string Email
    +string Password
    +string FirstName
    +string LastName
    +bool RememberMe
    +string? IpAddress
    +string? UserAgent
  }
  class RegisterCommandHandler {
    -UserManager~ApplicationUser~ _users
    -ITokenService _tokens
    -IApplicationDbContext _db
    +Handle(RegisterCommand, CancellationToken) Task~AuthTokensDto~
  }
  class RegisterCommandValidator {
    <<FluentValidation>>
    +Rule: Email format + max 256
    +Rule: Password >=8, 1 upper, 1 digit, 1 special
    +Rule: Names not empty, <=100
  }
  class AuthTokensDto {
    +string AccessToken
    +string RefreshToken
    +DateTimeOffset AccessTokenExpiresAt
    +DateTimeOffset RefreshTokenExpiresAt
    +CurrentUserDto User
  }
  class ValidationBehavior~T,R~ {
    -IEnumerable~IValidator~T~~ _validators
    +Handle throws ValidationException
  }
  class LoggingBehavior~T,R~ {
    -ILogger _logger
    -ICurrentUserService _currentUser
  }
  class PerformanceBehavior~T,R~ {
    -ILogger _logger
    +warns >500ms
  }

  IRequest~TResponse~ <|.. RegisterCommand : returns AuthTokensDto
  IRequestHandler~TRequest, TResponse~ <|.. RegisterCommandHandler
  IPipelineBehavior~TRequest, TResponse~ <|.. ValidationBehavior~T,R~
  IPipelineBehavior~TRequest, TResponse~ <|.. LoggingBehavior~T,R~
  IPipelineBehavior~TRequest, TResponse~ <|.. PerformanceBehavior~T,R~
  RegisterCommandHandler ..> RegisterCommand : handles
  RegisterCommandHandler ..> AuthTokensDto : produces
  RegisterCommandValidator ..> RegisterCommand : validates
```

## 4. Infrastructure adapters (contract + impls)

```mermaid
classDiagram
  class ITokenService {
    <<interface>>
    +IssueTokens(user, roles, activeRole, rememberMe) TokenPair
    +RotateRefreshTokenAsync(refreshToken, ipAddress, userAgent, ct) Task~TokenPair?~
    +RevokeRefreshTokenAsync(refreshToken, reason, ct) Task
    +RevokeAllForUserAsync(userId, reason, ct) Task
  }
  class IStripeService {
    <<interface>>
    +CreatePaymentIntentAsync(amount, currency, captureMethod, metadata, idempotency, ct)
    +CapturePaymentIntentAsync(intentId, amount, idempotency, ct)
    +CancelPaymentIntentAsync(intentId, reason, idempotency, ct)
    +RefundPaymentAsync(intentId, amount, reason, idempotency, ct)
    +CreateConnectAccountAsync(email, country, ct)
    +CreatePayoutAsync(account, amount, currency, idempotency, ct)
    +ParseWebhook(payload, signatureHeader, secret) StripeWebhookParseResult
  }
  class IAiService {
    <<interface>>
    +GenerateRecommendationsAsync(userId, topN, ct) AiRecommendationResult
    +CheckEligibilityAsync(userId, scholarshipId, ct) AiEligibilityResult
    +AskAsync(userId, sessionId, message, ct) AiChatResponse
  }
  class IEmailService {
    <<interface>>
    +SendAsync(EmailMessage, ct) Task
  }
  class INotificationDispatcher {
    <<interface>>
    +DispatchAsync(recipientId, type, content, deepLink, idempotency, ct)
    +DispatchBroadcastAsync(recipients, type, content, ct)
  }
  class IAuditService {
    <<interface>>
    +WriteAsync(action, targetType, targetId, beforeJson, afterJson, summary, ct)
  }

  class TokenService {
    -JwtOptions _opts
    -ApplicationDbContext _db
    -IDateTimeService _clock
  }
  class StubStripeService {
    <<dev-only>>
    returns fake intent/charge IDs
  }
  class StubAiService {
    <<dev-only>>
    returns canned responses + disclaimer
  }
  class StubEmailService {
    logs to console (MailHog in dev)
  }
  class StubNotificationDispatcher {
    logs dispatch attempts
  }
  class StubAuditService {
    logs audit events
  }

  ITokenService <|.. TokenService
  IStripeService <|.. StubStripeService
  IAiService <|.. StubAiService
  IEmailService <|.. StubEmailService
  INotificationDispatcher <|.. StubNotificationDispatcher
  IAuditService <|.. StubAuditService
```

## 5. Persistence (DbContext + Identity integration)

```mermaid
classDiagram
  class IdentityDbContext~TUser,TRole,TKey~ {
    <<ASP.NET Core>>
    +DbSet Users
    +DbSet Roles
    +DbSet UserRoles
    +DbSet UserClaims
  }
  class IApplicationDbContext {
    <<interface>>
    +DbSet~ApplicationUser~ Users
    +DbSet~UserProfile~ UserProfiles
    +DbSet~Scholarship~ Scholarships
    +DbSet~ApplicationTracker~ Applications
    +DbSet~ConsultantBooking~ Bookings
    +DbSet~Payment~ Payments
    +...30+ more DbSets
    +SaveChangesAsync(ct) Task~int~
  }
  class ApplicationDbContext {
    -IMediator? _mediator
    +OnModelCreating(ModelBuilder)
    +SaveChangesAsync(ct) override
    -DispatchDomainEventsAsync(ct)
  }
  class IEntityTypeConfiguration~T~ {
    <<EF Core>>
    +Configure(EntityTypeBuilder~T~)
  }
  class ApplicationUserConfiguration {
    HasQueryFilter(!IsDeleted)
    HasIndex(AccountStatus)
    HasOne(Profile).WithOne(User).HasForeignKey
  }
  class ScholarshipConfiguration {
    HasIndex(Slug).IsUnique
    HasIndex(Status, Deadline)
    HasQueryFilter(!IsDeleted)
  }
  class ApplicationTrackerConfiguration {
    HasIndex(StudentId, ScholarshipId).IsUnique.HasFilter(...)
    HasQueryFilter(!IsDeleted)
  }

  IdentityDbContext~TUser,TRole,TKey~ <|-- ApplicationDbContext
  IApplicationDbContext <|.. ApplicationDbContext
  IEntityTypeConfiguration~T~ <|.. ApplicationUserConfiguration
  IEntityTypeConfiguration~T~ <|.. ScholarshipConfiguration
  IEntityTypeConfiguration~T~ <|.. ApplicationTrackerConfiguration
```

## 6. Frontend architecture (stores + API + SignalR)

```mermaid
classDiagram
  class AuthState {
    <<Zustand store>>
    +CurrentUser? user
    +AuthTokens? tokens
    +bool isHydrated
    +setSession(user, tokens)
    +setTokens(tokens)
    +clear()
  }
  class UiState {
    <<Zustand store>>
    +ThemeMode theme
    +bool sidebarCollapsed
    +setTheme(mode)
    +toggleSidebar()
  }
  class ApiClient {
    <<axios instance>>
    +baseURL, timeout 30s
    +interceptor: attach JWT + Accept-Language
    +interceptor: 401 -> refresh -> retry
  }
  class ApiError {
    +number status
    +ApiErrorPayload payload
  }
  class QueryClient {
    <<TanStack Query>>
    +defaultOptions: staleTime, gcTime, retry
  }
  class QueryKeys {
    <<factory>>
    +auth.me
    +scholarships.list(filters)
    +scholarships.detail(id)
    +applications.mine
    +consultants.directory(filters)
  }
  class NotificationHub {
    <<SignalR client wrapper>>
    +createConnection()
    +accessTokenFactory: () => authStore.tokens.accessToken
    +withAutomaticReconnect
  }
  class ChatHub {
    <<SignalR client wrapper>>
    +JoinConversation(id)
    +LeaveConversation(id)
    +TypingStart / TypingStop
  }
  class useNotificationHub {
    <<React hook>>
    subscribes while tokens exist
    on("notification") -> toast
  }
  class useScholarshipsQuery {
    <<React hook>>
    wraps useQuery with queryKeys
    placeholderData: keep previous
  }

  ApiClient ..> AuthState : reads tokens
  useNotificationHub ..> NotificationHub : creates
  useNotificationHub ..> AuthState : reads tokens
  useScholarshipsQuery ..> QueryClient
  useScholarshipsQuery ..> QueryKeys
  useScholarshipsQuery ..> ApiClient
```

---

## How these diagrams map to code

| Diagram | Primary file(s) |
|---|---|
| 1. Clean Architecture projects | `server/ScholarPath.slnx`, the `.csproj` files |
| 2. Domain aggregates | `server/src/ScholarPath.Domain/Entities/*.cs`, `Common/BaseEntity.cs` |
| 3. CQRS vertical slice | `server/src/ScholarPath.Application/Auth/Commands/Register/*.cs` |
| 4. Infrastructure adapters | `server/src/ScholarPath.Application/Common/Interfaces/*.cs`, `server/src/ScholarPath.Infrastructure/Services/*.cs` |
| 5. Persistence | `server/src/ScholarPath.Infrastructure/Persistence/ApplicationDbContext.cs` + `Configurations/EntityConfigurations.cs` |
| 6. Frontend architecture | `client/src/stores/`, `client/src/services/api/`, `client/src/services/signalR/`, `client/src/hooks/` |

---

## Rendering

GitHub renders `classDiagram` natively inside `.md` files. If you need higher-quality exports for a printed defense booklet, paste the Mermaid blocks into https://mermaid.live and export as SVG/PNG.
