# Architecture

ScholarPath is a gated scholarship platform split into three deployable artifacts and one design contract.

## Layers

```
┌─────────────────────────────────────────────────┐
│  client/  (React 19 + Vite + Tailwind v4)       │  ── Static SPA
│  ├─ routes/           layouts + guards          │
│  ├─ pages/            module screens            │
│  ├─ components/       shared UI + shadcn        │
│  ├─ stores/           Zustand (auth, ui)        │
│  ├─ services/api/     axios REST client         │
│  ├─ services/signalR/ SignalR clients           │
│  └─ lib/i18n/         EN + AR + RTL             │
└─────────────────────────────────────────────────┘
                 │  HTTPS (JWT Bearer)
                 ▼
┌─────────────────────────────────────────────────┐
│  server/ScholarPath.API  (ASP.NET Core 10)     │  ── HTTP + SignalR host
│  ├─ Controllers/     thin pass-throughs         │
│  ├─ Middleware/      exception, headers, cors   │
│  ├─ Filters/         request filters            │
│  └─ Program.cs       DI + pipeline + SignalR    │
└─────────────────────────────────────────────────┘
                 │  MediatR IRequest → IRequestHandler
                 ▼
┌─────────────────────────────────────────────────┐
│  ScholarPath.Application                        │  ── Business logic
│  ├─ Common/Behaviors/ Validation, Logging, Perf │
│  ├─ Common/Interfaces/ IApplicationDbContext,   │
│  │                     IStripeService, IAi...   │
│  └─ 14 module slices/ Commands/Queries/DTOs     │
└─────────────────────────────────────────────────┘
          │                                │
          ▼                                ▼
┌───────────────────────┐   ┌─────────────────────────────────┐
│  ScholarPath.Domain   │   │  ScholarPath.Infrastructure     │
│  Entities (40)        │   │  EF DbContext + configs         │
│  Enums, Events        │   │  Identity, JWT, SSO             │
│  Interfaces           │   │  Stripe, Email, Blob, AI (stubs)│
│  Zero deps (except    │   │  SignalR hubs, Hangfire jobs    │
│  Identity POCOs)      │   │  Serilog, Azure Key Vault       │
└───────────────────────┘   └─────────────────────────────────┘
                                        │
                                        ▼
                            ┌─────────────────────────┐
                            │  SQL Server 2022        │
                            │  Redis (flag-gated)     │
                            │  MailHog / SendGrid     │
                            │  Azure Blob             │
                            │  Stripe, OpenAI         │
                            └─────────────────────────┘
```

Constitutional rule: dependencies flow inward only. Domain has no EF dependency. Application depends on Domain + MediatR + FluentValidation. Infrastructure implements Application interfaces. API wires everything via DI.

## Request lifecycle (typical write)

```
 HTTP POST /api/bookings           [JWT bearer]
   ↓ AuthenticationMiddleware      (validates JWT)
   ↓ CORS / SecurityHeaders
   ↓ RateLimiter
   ↓ SerilogRequestLogging
   ↓ ExceptionHandlerMiddleware    (catches → RFC 7807)
   ↓ MVC ModelBinding              (RequestDto)
   ↓ Controller → _mediator.Send(RequestBookingCommand)
      ↓ ValidationBehavior          (FluentValidation)
      ↓ LoggingBehavior             (Serilog enter/exit)
      ↓ PerformanceBehavior         (>500ms warnings)
      ↓ RequestBookingCommandHandler
         ├─ load IApplicationDbContext
         ├─ call IStripeService.CreatePaymentIntentAsync (hold)
         ├─ persist ConsultantBooking row
         ├─ raise BookingRequestedEvent (domain event)
         └─ return BookingDto
      ↓ DbContext.SaveChangesAsync
         └─ DispatchDomainEventsAsync → mediator.Publish
            └─ NotificationEventHandler → dispatcher.SendAsync
               ├─ persist Notification row
               └─ Hub.Clients.Group(...).SendAsync("notification", ...)
 ← ActionResult + status 202
```

## Event flow (domain events)

Domain events are raised on aggregate roots and dispatched by `ApplicationDbContext.SaveChangesAsync` through MediatR `Publish`. Module-owned handlers subscribe and fan out to side effects (notifications, emails, hub broadcasts). This keeps handlers thin and module-aligned.

## Real-time

Three SignalR hubs — all require JWT bearer and auto-add the connection to a `user:{userId}` group for targeted pushes.

| Hub                  | Purpose                               |
|----------------------|---------------------------------------|
| `/hubs/notifications`| Per-user toasts, bell updates         |
| `/hubs/chat`         | 1:1 conversations + presence + typing |
| `/hubs/community`    | Forum live posts + reply broadcasts   |

Redis backplane is supported but off by default (see `Redis:Enabled` flag).

## State machines

| Entity                | States                                                                 |
|-----------------------|-----------------------------------------------------------------------|
| `ApplicationUser`     | Unassigned → PendingApproval → Active → Suspended → Deactivated       |
| `ApplicationTracker`  | Draft → Pending → UnderReview → Shortlisted → Accepted/Rejected/Withdrawn |
| `ConsultantBooking`   | Requested → Confirmed → Completed / Cancelled / NoShow*               |
| `Payment`             | Pending → Held → Captured → Refunded/PartiallyRefunded                |
| `Resource`            | Draft → PendingReview → Published → Hidden/Removed                    |

Each transition is enforced by the owning module's state guard. Accepted/Rejected apps become `IsReadOnly=true`.

## Data integrity invariants

- **Single-active-application rule** (FR-057): unique filtered index on `ApplicationTracker (StudentId, ScholarshipId) WHERE Status NOT IN (Withdrawn, Rejected, Accepted)` — at the SQL level.
- **Payment idempotency**: unique on `Payment.IdempotencyKey` + `StripeWebhookEvent.StripeEventId`.
- **Soft delete**: every business entity has `IsDeleted` + global query filter. `EF Core` `HasQueryFilter` drops them from all reads.
- **Audit**: every mutation decorated `[Auditable]` writes to `AuditLog`.
- **Domain events**: raised on entity changes, published after a successful `SaveChangesAsync`.

## Further reading
- [`AUTH.md`](./AUTH.md) — auth flows + sequence diagrams
- [`PAYMENTS.md`](./PAYMENTS.md) — Stripe flows + refund matrix
- [`RTL.md`](./RTL.md) — Arabic + RTL conventions
- [`TESTING.md`](./TESTING.md) — testing strategy
- [`DESIGN.md`](./DESIGN.md) — visual design system
- [`CHROME-DEVTOOLS-MCP.md`](./CHROME-DEVTOOLS-MCP.md) — visual QA tooling
