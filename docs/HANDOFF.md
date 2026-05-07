# Team onboarding — ScholarPath

You've just joined a module on ScholarPath. Read this page end-to-end before your first commit.

## Status snapshot — May 2026

Beyond the initial scaffold, three modules have been shipped end-to-end by @ma7moudalysalem:

| Module | Status | Highlights |
|---|---|---|
| **PB-012 Audit & Compliance** | ✅ complete | `[Auditable]` attribute + MediatR `AuditBehavior` • `IAuditService` real impl • Hangfire data export + 30-day delete jobs • daily `IntegrityCheckJob` • admin `GetAuditLogQuery` + viewer UI • student `DataPrivacy` page |
| **PB-011 Admin Portal** | ✅ backend + core frontend (5 tasks delegated to other module owners) | `IUserAdministration` abstraction over `UserManager` • user search + status management + role commands • onboarding + upgrade queues • analytics (overview + growth + funnel + standalone page) • broadcast • audit log viewer • full AR copy |
| **PB-008 AI Features** | ✅ complete | `IAiService` with two providers: `LocalAiService` (deterministic, default) + `OpenAiService` (real gpt-4o-mini behind `Ai:Provider` flag) • `AiCostGate` daily budget • source-generated PII redaction • recommendations (cached GET + regenerate POST) + eligibility modal + chatbot + disclaimer + match-score badge |

**Codebase metrics** as of 2026-05-06:
- 34 commits on `main`
- 52 unit + integration tests green (0 failing, 0 warnings)
- 9 Playwright smoke tests (route guards)
- Full EN + AR copy across 9 namespaces
- 0 `dotnet build` warnings, 0 TypeScript errors

**What this unblocks for the rest of the team**:
- Every command you write gets auto-audited by adding `[Auditable(AuditAction.X, "TargetType")]` — zero boilerplate in the handler.
- Every admin-side mutation of your entities hits the audit trail automatically when routed through PB-011.
- The AI disclaimer component and match-score badge are reusable (`client/src/components/ai/`) — wire them into your scholarship detail / student dashboard when you build those pages.
- `IUserAdministration.RevokeAllSessionsAsync(userId, reason, ct)` is the primitive for any "force logout" logic (e.g. Mimi's upcoming `LogoutCommand`, password-change revocation, etc.).

## What's already built (the foundation)

### Planning artifacts
- `.specify/memory/constitution.md` — **8 non-negotiable principles**. Every PR is reviewed against this.
- `.specify/specs/PB-001..PB-014/` — 42 files (spec.md + plan.md + tasks.md per module). Each module has an **owner tag**.
- `docs/SRS.md` — full Software Requirements Specification (~5K lines, 211 FRs, 203 use cases, 159 user stories).

### Backend (`server/`) — 100% buildable
- Clean Architecture solution: **Domain / Application / Infrastructure / API** + 2 test projects.
- **40 entities** with configurations, FKs, unique indexes, soft-delete filters, domain events.
- Initial EF migration (`InitialSchema`) — 80+ tables including 7 Identity tables.
- **ASP.NET Core Identity** (custom `ApplicationUser : IdentityUser<Guid>`) + 5 seeded roles.
- **JWT + refresh rotation + lockout** + Google/Microsoft SSO stubs.
- Middleware pipeline: Exception handler (RFC 7807), security headers, CORS, Serilog, rate limiting.
- **Scalar** at `/scalar/v1` + Swashbuckle OpenAPI.
- **3 SignalR hubs** (Chat, Notification, Community) authenticated with JWT.
- **Hangfire** wired with feature flag off (jobs stubbed).
- Stripe / Email / Blob / AI / Audit / Notification stubs behind interfaces.
- **DB seeder** with 4 demo users (see accounts table below).
- Smoke unit tests passing. 0 warnings, 0 errors in `dotnet build`.

### Frontend (`client/`) — 100% buildable + runnable
- Vite 8 + React 19 + TypeScript 6 + Tailwind v4 + shadcn/Radix + Motion.
- Apple-inspired design system tokens: typography, colors, shadows, motion, dark mode, RTL, pill CTAs.
- **Full i18n EN + AR** with 6 namespaces and RTL direction flip.
- Zustand auth + ui stores; axios client with **401→refresh** interceptor.
- TanStack Query client + `queryKeys` factory + 1 reference query hook.
- SignalR typed client wrappers + live notification-to-toast hook wired into `AuthenticatedLayout`.
- **Stripe Elements** demo component ready for the consultant booking checkout page.
- `<EmptyState owner module specPath>` placeholder on **40+ module route stubs** so every page renders.
- Auth pages (Login, Register, ForgotPassword, ResetPassword, OnboardingWizard, SsoCallback) — UI complete, awaiting backend wiring.
- Full hero Home page with animated pillars section.
- Vitest + Playwright configured with smoke specs.

### Dev + deploy
- `docker-compose.yml` with sqlserver + redis + mailhog (+ optional `--profile full` for api + client).
- `Dockerfile.api`, `Dockerfile.client` (Node build → Nginx serve).
- `.github/workflows/ci.yml` — change-filtered matrix; backend + client + security lanes.
- `.github/workflows/deploy.yml` — environment-gated deploy template (Azure-ready, commented).
- `.github/CODEOWNERS` — auto-requests your owner review for PRs in your module.
- `.vscode/` — launch, tasks, extensions.json recommendations.

### Docs
- `docs/ARCHITECTURE.md` — layers + request lifecycle + state machines + invariants
- `docs/AUTH.md` — flows + role matrix + token rotation
- `docs/PAYMENTS.md` — Stripe lifecycle + refund matrix + webhook idempotency
- `docs/RTL.md` — Arabic + RTL conventions + logical properties
- `docs/TESTING.md` — pyramid + examples + coverage targets
- `docs/CHROME-DEVTOOLS-MCP.md` — visual QA setup
- `docs/DESIGN.md` — design system tokens + components

## Demo accounts (dev-only)

| Role       | Email                           | Password      |
|------------|---------------------------------|---------------|
| Admin      | `admin@scholarpath.local`       | `Admin123!`   |
| Student    | `student@scholarpath.local`     | `Student123!` |
| Company    | `company@scholarpath.local`     | `Company123!` |
| Consultant | `consultant@scholarpath.local`  | `Consult123!` |

## Team + what you own

| ID     | Module                                  | Owner        | Iteration |
|--------|-----------------------------------------|--------------|-----------|
| PB-001 | Auth, Access, Onboarding                | **@Madiha6776**    | 1         |
| PB-002 | Profile and Account Management          | **@Madiha6776**    | 1         |
| PB-003 | Scholarship Discovery + Listing         | **@norra-mmhamed**    | 2         |
| PB-004 | In-App Application + External Tracking  | **@norra-mmhamed**    | 2         |
| PB-005 | Company Review, Payment, Rating         | **@yousra-elnoby**   | 3         |
| PB-006 | Consultant Booking, Payment, Rating     | **@TasneemShaaban** | 3         |
| PB-007 | Community + Chat                        | **@yousra-elnoby**   | 4         |
| PB-008 | AI Features                             | **@ma7moudalysalem** | 4         |
| PB-009 | Resources Hub                           | **@yousra-elnoby**   | 4         |
| PB-010 | Notifications                           | **@Madiha6776**    | 3         |
| PB-011 | Admin Portal                            | **@ma7moudalysalem** | 1         |
| PB-012 | Audit & Compliance                      | **@ma7moudalysalem** | 4         |
| PB-013 | Payment Processing + Settlement         | **@norra-mmhamed**    | 2         |
| PB-014 | Portal Profit Share                     | **@TasneemShaaban** | 3         |

**Mahmoud** = team lead + architect + AI. Reviews shared infrastructure + Program.cs + DbContext + migrations + CI + design system + all cross-module touches.

## What each teammate tackles first

### @Madiha6776 — starts with PB-001 (Auth)
1. Open `.specify/specs/PB-001-auth-access-onboarding/tasks.md` and work the list top-down.
2. The `AuthController` has 14 stub endpoints; each returns a 404 with a guiding message. Replace each stub with a real MediatR `Send`.
3. Pattern per endpoint:
   - `Commands/<UseCase>/<Command>.cs` — a reference `RegisterCommand` already exists
   - `Commands/<UseCase>/<Command>Validator.cs` — reference `RegisterCommandValidator` with password rules
   - `Commands/<UseCase>/<Command>Handler.cs` — you write this; uses `UserManager<ApplicationUser>` + `ITokenService`
4. On the Login page frontend, replace the TODO with `apiClient.post("/api/auth/login", ...)` and store tokens in `authStore`.
5. When auth is green end-to-end, start on PB-002 (Profile) — it blocks AI recommendations.

### @norra-mmhamed — starts with PB-003 (Scholarships)
1. Open `.specify/specs/PB-003-scholarship-discovery/tasks.md`.
2. Write `SearchScholarshipsQuery` + handler. Use the reference pattern from `useScholarshipsQuery.ts` on the frontend.
3. Seed some scholarship test data in `DbSeeder.cs` for immediate UI iteration.
4. PB-004 (Applications) — the single-active-application filtered unique index is already in the migration. Write handlers that respect the constraint (409 on conflict).
5. PB-013 (Payments) — `IStripeService` + `StubStripeService` + webhook endpoint already exist. Replace the stub with real Stripe.net calls.

### @yousra-elnoby — starts with PB-007 (Community + Chat)
1. Open `.specify/specs/PB-007-community-chat/tasks.md`.
2. SignalR hubs are already authenticated. Write the `SendMessageCommand` and subscribe to `ChatHub.on("MessageReceived")` on the frontend.
3. PB-005 (Company Review + Rating) — review/rating flow after application decision. Stripe handling is in PB-013; your part is the review lifecycle + Company-facing UI.
4. PB-009 (Resources Hub) — schema exists; build the Draft → PendingReview → Published state machine + moderation UI.

### @TasneemShaaban — starts with PB-006 (Consultant Booking)
1. Open `.specify/specs/PB-006-consultant-booking/tasks.md`.
2. This is the hardest module: Stripe hold/capture/refund + booking lifecycle + availability management + SignalR updates.
3. Implement `RefundCalculatorService` as a pure function first, unit-test the entire matrix from `docs/PAYMENTS.md`, then wire it in.
4. PB-014 (Profit Share) is lighter — percentage config + calculator + admin UI. Do this after the booking flow is stable.

### @ma7moudalysalem — team lead + architect + AI
1. PB-011 (Admin Portal) — ship the shell + user search first; analytics widgets fill in as modules become ready. Every admin mutation must call `IAuditService`.
2. PB-012 (Audit) — the `[Auditable]` MediatR pipeline behavior goes on decorated commands. 30-day delayed delete job via Hangfire.
3. PB-008 (AI) — `StubAiService` returns canned responses. Replace with the chosen provider (OpenAI or Azure OpenAI). Every response must carry the AI disclaimer (FR-121).
4. Ongoing: review every PR from the team, enforce constitution rules, own shared infra files (CODEOWNERS auto-requests me there).

## What's intentionally deferred

- **Real Stripe Connect onboarding UI** (Nora — once test-mode payment flow is solid).
- **OpenAI / Azure OpenAI wiring** (Mahmoud — stub is fine for dev; wire before demo).
- **Azure Key Vault live secrets** (deploy phase — `secrets.json` is fine locally).
- **Redis live backplane** (in-memory is fine for now).
- **SendGrid account** (MailHog catches emails at `http://localhost:8025`).
- **Push notifications** (Iteration 3+).
- **Multi-currency / multi-payout logic** (v2).

## Constitution compliance — quick checklist

Before opening a PR, verify:
- [ ] Gated: any new route is `[Authorize]` (backend) + inside `<RequireAuth>` (frontend), unless it's truly public.
- [ ] Clean Arch: you didn't import `Microsoft.EntityFrameworkCore` inside `Application/`; only interfaces live there.
- [ ] Traceability: your commit + PR title references FR-xxx or US-xxx.
- [ ] Bilingual: every user-facing string has both an EN and AR translation.
- [ ] Test coverage: unit tests for every handler, integration test for every controller action.
- [ ] Security: no secrets committed, no passwords logged, Stripe webhook handlers are idempotent.
- [ ] Observability: handlers log structured entries; mutations raise audit events.
- [ ] Empty page: if you add a route, either implement it or keep an `<EmptyState>` placeholder with owner tag.

## Running locally (verified)

```bash
# Infrastructure
docker compose up -d sqlserver redis mailhog

# Backend
cd server
dotnet ef database update --project src/ScholarPath.Infrastructure --startup-project src/ScholarPath.API
dotnet run --project src/ScholarPath.API
# Scalar → http://localhost:5000/scalar/v1
# Health → http://localhost:5000/health

# Frontend (new terminal)
cd client
npm ci
npm run dev
# App   → http://localhost:5173
# MailHog → http://localhost:8025

# Tests
cd server && dotnet test
cd client && npm run typecheck && npm run lint && npm test
```

## Known gotchas (save yourself some debugging)

1. **MediatR v14** prints a dev-license notice on every run. Ignore in development; production will need a commercial license review.
2. **AutoMapper pinned to 14.0.0** — v15+ is commercial. Don't upgrade without a license review.
3. **Tailwind v4** uses the `@theme` directive inside CSS — there is **no** `tailwind.config.js` for tokens. See `client/src/theme/globals.css`.
4. **Domain events** only fire after `SaveChangesAsync` succeeds. If you want side effects, raise the event in the aggregate; don't run side-effect code inline.
5. **Soft delete** is global. If you need to query deleted rows (admin restore), use `db.Xs.IgnoreQueryFilters()`.
6. **CA analyzer warnings** are suppressed pragmatically in `Directory.Build.props`. If you add new types, check the NoWarn list before introducing a violation.
7. **Single-active-application rule** is a **filtered unique index** at SQL level. A duplicate active application throws `DbUpdateException` — catch and translate to 409.
8. **Stripe webhook**: always verify signature + check `StripeWebhookEvent.StripeEventId` uniqueness before processing. Replay tests are documented in `docs/PAYMENTS.md`.

## Where to ask

- **Spec questions**: open an issue in the `.specify/specs/PB-xxx/` folder; tag the owner.
- **Cross-module changes**: at least 2 owner approvals on the PR (CODEOWNERS enforces).
- **Constitution amendments**: PR to `.specify/memory/constitution.md` with rationale; 2-of-3 owner approval.
- **Architecture / shared infra / blockers**: ping @ma7moudalysalem in the group chat.

Good luck. Keep the structure disciplined and we ship in 4 iterations.
