# ScholarPath Architecture

## Overview

ScholarPath follows **Clean Architecture** (also referred to as Onion Architecture), ensuring a clear separation of concerns, testability, and independence from external frameworks. The dependency rule is strict: inner layers never depend on outer layers. All dependencies point inward toward the Domain layer.

The solution is organized into four distinct projects:

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `ScholarPath.Domain` | Entities, enums, value objects, domain interfaces |
| Application | `ScholarPath.Application` | CQRS commands/queries with MediatR handlers, DTOs, validators, service interfaces |
| Infrastructure | `ScholarPath.Infrastructure` | EF Core DbContext, token service, caching (Redis), background jobs (Hangfire) |
| API | `ScholarPath.API` | Controllers, middleware pipeline, DI composition root, Swagger |

---

## Layer Dependency Diagram

```mermaid
graph TB
    subgraph Outer["Presentation"]
        API["API Layer<br/>(Controllers, Middleware,<br/>DI Registration)"]
    end

    subgraph Middle["Infrastructure"]
        INFRA["Infrastructure Layer<br/>(EF Core, Repositories,<br/>External Services)"]
    end

    subgraph Inner["Application"]
        APP["Application Layer<br/>(CQRS Handlers, DTOs,<br/>Validators, Interfaces)"]
    end

    subgraph Core["Domain"]
        DOM["Domain Layer<br/>(Entities, Enums,<br/>Value Objects)"]
    end

    API -->|"uses"| APP
    API -->|"registers DI"| INFRA
    INFRA -->|"implements"| APP
    APP -->|"depends on"| DOM

    style Core fill:#2d6a4f,stroke:#1b4332,color:#fff
    style Inner fill:#40916c,stroke:#2d6a4f,color:#fff
    style Middle fill:#52b788,stroke:#40916c,color:#fff
    style Outer fill:#95d5b2,stroke:#52b788,color:#000
```

### Dependency Rules

1. **Domain Layer** has a single external dependency: `Microsoft.Extensions.Identity.Stores` (for `IdentityUser<Guid>` base class). It defines entities, enums, and domain-level interfaces.
2. **Application Layer** depends on Domain. It defines use-case logic via MediatR handlers, FluentValidation validators, and service/repository interfaces.
3. **Infrastructure Layer** depends on Application (to implement its interfaces). It never exposes its own abstractions upward.
4. **API Layer** depends on Application (to send commands/queries) and references Infrastructure only at the composition root for dependency injection registration.

---

## Request Flow

Every HTTP request follows a predictable pipeline from the client through the layers and back.

```mermaid
sequenceDiagram
    participant Client
    participant Middleware as API Middleware<br/>(Auth, Validation, Error Handling)
    participant Controller as API Controller
    participant MediatR
    participant Handler as Application Handler
    participant Repo as Repository / Service
    participant DB as Database

    Client->>Middleware: HTTP Request
    Middleware->>Controller: Routed Request
    Controller->>MediatR: Send Command / Query
    MediatR->>Handler: Dispatch to Handler
    Handler->>Repo: Call Repository or Service
    Repo->>DB: Execute SQL via EF Core
    DB-->>Repo: Result Set
    Repo-->>Handler: Domain Entities / DTOs
    Handler-->>MediatR: Response DTO
    MediatR-->>Controller: Response DTO
    Controller-->>Middleware: HTTP Response
    Middleware-->>Client: JSON Response
```

### Pipeline Behaviors

MediatR pipeline behaviors intercept every command/query before it reaches the handler:

1. **ValidationBehavior** -- Runs FluentValidation rules. Returns 400 if validation fails.
2. **LoggingBehavior** -- Logs request metadata for observability.
3. **Handlers** -- Execute business logic. Read handlers use `.AsNoTracking()` and Redis caching. Write handlers validate state transitions and handle `DbUpdateException` for concurrency.

---

## Technology Stack by Layer

| Layer | Technologies |
|---|---|
| **Domain** | C# 13, .NET 10, Microsoft.Extensions.Identity.Stores (IdentityUser) |
| **Application** | MediatR (CQRS), FluentValidation, AutoMapper |
| **Infrastructure** | Entity Framework Core 10 (SQL Server + SQLite), ASP.NET Identity, JWT Bearer Auth, Redis (StackExchange.Redis), Hangfire, Serilog, Newtonsoft.Json (Hangfire transitive dependency override) |
| **API** | ASP.NET Core 10 Web API, Swagger/OpenAPI, SignalR (ASP.NET Core shared framework), API Versioning (Asp.Versioning.Mvc), Health Checks |
| **Cross-Cutting** | Serilog (logging), Docker, GitHub Actions (CI/CD) |

---

## Project Structure

```
ScholarPath/
  server/
    src/
      ScholarPath.Domain/
        Common/           # BaseEntity, AuditableEntity, ISoftDeletable
        Entities/         # ApplicationUser, Scholarship, ApplicationTracker, etc.
        Enums/            # UserRole, AccountStatus, ApplicationStatus, etc.
        Interfaces/       # IApplicationDbContext, ICachingService, ITokenService
      ScholarPath.Application/
        Auth/
          Commands/       # Register, Login, Logout, Refresh, ForgotPassword, LinkProvider, etc.
          Queries/        # GetMe
          DTOs/
          Validators/
        Scholarships/
          Commands/       # SaveScholarship, DeleteSavedScholarship
          Queries/        # SearchScholarships, GetScholarshipDetail, GetRecommendedScholarships, GetSavedScholarships
          DTOs/
        Applications/
          Commands/       # TrackApplication, UpdateApplicationStatus, DeleteApplicationTracker
          Queries/        # GetApplications
          DTOs/
        Dashboard/
          Queries/        # GetDashboardSummary
          DTOs/
        Admin/
          Commands/       # ApproveUpgradeRequest, RejectUpgradeRequest, RequestMoreInfo
          Queries/
        Common/           # PaginatedResponse, Result, ICachingService interface
      ScholarPath.Infrastructure/
        Persistence/
          ApplicationDbContext.cs
          Configurations/  # EF Core entity configurations
          Seeds/           # SeedData.cs
        Migrations/
        Services/          # TokenService, EmailService, CachingService
        Settings/          # JwtSettings, RedisSettings
      ScholarPath.API/
        Controllers/       # AuthController, ExternalAuthController, ScholarshipsController, ApplicationsController, AdminController, DashboardController
        Middleware/        # SecurityHeadersMiddleware, ExceptionHandlingMiddleware
    tests/
      ScholarPath.UnitTests/
      ScholarPath.IntegrationTests/
  client/
  docs/
  docker-compose.yml
```

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Clean Architecture | Enforces testability and framework independence |
| CQRS via MediatR | Separates read and write concerns; all controllers delegate to MediatR handlers |
| FluentValidation | Declarative validation rules, separated from handler logic |
| EF Core with SQL Server | Mature ORM with strong migration support |
| ASP.NET Identity | Built-in user management, password hashing, role-based auth |
| HttpOnly Cookie Auth | JWT access token + refresh token stored in HttpOnly cookies; prevents XSS token theft |
| Redis Caching | Read-heavy query handlers (recommendations, scholarship detail, dashboard) cache results with short TTL |
| `.AsNoTracking()` on queries | All read-only handlers use `AsNoTracking()` to reduce EF Core overhead |
| Soft Deletes | Data recovery and audit compliance via `ISoftDeletable` |
| Relational child tables | `ScholarshipEligibleCountry`, `ScholarshipEligibleMajor`, `ScholarshipDocumentItem` replaced JSON columns for queryability (D2 audit fix) |
| Domain IdentityUser Dependency | Pragmatic trade-off: Domain depends on `Microsoft.Extensions.Identity.Stores` to use `IdentityUser<Guid>` as the base class for `ApplicationUser`, avoiding a separate mapping layer |
| SignalR via Shared Framework | SignalR is part of `Microsoft.AspNetCore.App` shared framework — no separate NuGet package required, referenced via `<FrameworkReference>` in Infrastructure |
