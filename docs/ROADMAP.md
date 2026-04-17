# Roadmap & Project Journey

**Last updated**: 2026-04-17
**Current tag**: `v2.0.0-scaffold` (pending)
**Repo**: https://github.com/ma7moudalysalem/ScholarPath

This document is the narrative companion to `docs/HANDOFF.md`. It answers:

1. What did we set out to build?
2. How did we plan it?
3. What have we built so far?
4. What's next — week by week, iteration by iteration?
5. What academic artifacts exist and where to find them?

---

## 1. What we're building

**ScholarPath** is a gated, bilingual (EN + AR with RTL) scholarship platform. Students, scholarship providers (Companies), verified Consultants, and Admins each interact with a role-specific workspace. The full scope is captured in **[`docs/SRS.md`](./SRS.md)** — 211 functional requirements, 203 use cases, 159 user stories across 14 product-backlog modules (PB-001 … PB-014).

### Headline features
- Personalized scholarship discovery with AI-ranked match scores
- In-app + external application tracking on a unified kanban
- 1:1 Consultant booking with Stripe-held payments + refund matrix
- Real-time community forum (auto-hide on 3 flags) + 1:1 chat (SignalR)
- Curated Resources Hub (articles, guides, checklists) with moderation
- AI assistant for eligibility + chatbot + essay feedback
- Full admin oversight: approvals, moderation, financial config, audit

### Non-functional requirements we commit to (SRS §5)
- Page load < 2s, search < 500ms over 100K listings, 5K concurrent users
- AI response < 3s, chat < 200ms
- TLS 1.2+, PBKDF2 hashing, RS256 JWT, Stripe signature verification, 99.5% uptime
- EN + AR with RTL from day 1, ≥70% test coverage

---

## 2. How we planned it

### Spec-driven development
Before writing a single line of code, we built a planning substrate:

1. **Constitution** (`.specify/memory/constitution.md`) — 8 non-negotiable principles. Every PR is reviewed against it.
2. **14 module specs** (`.specify/specs/PB-xxx-*/`) — each module has three markdown files:
   - `spec.md` — user stories + acceptance criteria distilled from the SRS
   - `plan.md` — architecture touchpoints (entities, endpoints, UI pages, events)
   - `tasks.md` — ordered checklist with owner tag
3. **Dependency graph** documented so iteration order is explicit.

### Architecture-first
Then we drew the Clean Architecture layers and decided: Domain has zero external deps, Application depends only on Domain + MediatR abstractions, Infrastructure implements Application's interfaces, API wires it all. This was enforced by project references in the .csproj files so the compiler rejects violations.

### Iteration model
The SRS backlog defines 4 iterations. We committed to:
- Iteration 1: Auth, Profile, Admin shell → unblocks everyone
- Iteration 2: Scholarships, Applications, Payments → core loops
- Iteration 3: Consultant Booking, Company Review, Notifications, Profit Share → revenue + ops
- Iteration 4: Community+Chat, AI, Resources, Audit → polish + differentiators

---

## 3. What's built so far (scaffold complete)

Every cell in this table is ✅ implemented or 🟡 stubbed with a concrete plan for the owner.

### Planning artifacts
| Item | Status |
|---|---|
| Constitution (8 principles) | ✅ `.specify/memory/constitution.md` |
| 14 module specs (spec + plan + tasks = 42 files) | ✅ `.specify/specs/` |
| SRS reference (full 211 FRs) | ✅ `docs/SRS.md` |
| This roadmap | ✅ `docs/ROADMAP.md` |
| ERD (entity relationships) | ✅ `docs/ERD.md` |
| ERD mapping (entity→table, constraints) | ✅ `docs/ERD-MAPPING.md` |
| Class diagrams (UML) | ✅ `docs/CLASS-DIAGRAMS.md` |
| Architecture deep dive | ✅ `docs/ARCHITECTURE.md` |
| Auth flows | ✅ `docs/AUTH.md` |
| Payment flows + refund matrix | ✅ `docs/PAYMENTS.md` |
| RTL + i18n conventions | ✅ `docs/RTL.md` |
| Testing strategy | ✅ `docs/TESTING.md` |
| Visual QA with Chrome DevTools MCP | ✅ `docs/CHROME-DEVTOOLS-MCP.md` |
| Design system | ✅ `docs/DESIGN.md` |
| Team onboarding | ✅ `docs/HANDOFF.md` |

### Backend (`server/`)
| Layer | What's in | Where |
|---|---|---|
| Solution structure | .NET 10 Clean Arch (4 projects + 2 test projects) | `server/ScholarPath.slnx` |
| Central version management | All packages pinned via `Directory.Packages.props` | `server/Directory.Packages.props` |
| Domain entities | 40 entities, enums, events, POCOs | `server/src/ScholarPath.Domain/` |
| Application pipeline | MediatR 14 + 3 pipeline behaviors + 14 module folders | `server/src/ScholarPath.Application/` |
| DbContext + configs | IdentityDbContext<T> + 40+ IEntityTypeConfigurations | `server/src/ScholarPath.Infrastructure/Persistence/` |
| Initial EF migration | 80+ tables including 7 Identity tables | `server/src/ScholarPath.Infrastructure/Migrations/` |
| JWT auth | RS256-ready + refresh rotation + replacement chain | `server/src/ScholarPath.Infrastructure/Services/TokenService.cs` |
| SSO (Google + Microsoft) | Stub; real impl wired in Program.cs | `server/src/ScholarPath.Infrastructure/Services/StubServices.cs` |
| SignalR hubs (3) | ChatHub, NotificationHub, CommunityHub (JWT auth) | `server/src/ScholarPath.Infrastructure/Hubs/` |
| Hangfire jobs | 5 jobs stubbed (feature-flagged off) | `server/src/ScholarPath.Infrastructure/Jobs/` |
| Stripe integration | IStripeService + StubStripeService + webhook handler | `server/src/ScholarPath.Infrastructure/` |
| Email / Blob / AI / Notifications / Audit | All interfaces + stub implementations | `server/src/ScholarPath.Infrastructure/Services/` |
| DB seeder | 5 roles + 4 demo users + 4 categories + 2 profit-share configs | `server/src/ScholarPath.Infrastructure/Persistence/Seed/` |
| Scalar + Swashbuckle OpenAPI | `/scalar/v1` loads full API docs | `server/src/ScholarPath.API/Program.cs` |
| Middleware | ExceptionHandler (RFC 7807), SecurityHeaders, Serilog, CORS, rate limit | `server/src/ScholarPath.API/Middleware/` |
| Smoke unit tests | 8 tests passing | `server/tests/ScholarPath.UnitTests/Smoke/` |
| Build status | `dotnet build` — 0 warnings / 0 errors | verified in CI |

### Frontend (`client/`)
| Area | What's in | Where |
|---|---|---|
| Stack versions | React 19.2, Vite 8, TS 6, Tailwind v4.2, shadcn/Radix, Motion 12, TanStack Query 5, Zustand 5 | `client/package.json` |
| Design system | Apple-inspired tokens (brand, typography, shadows, motion, dark mode) | `client/src/theme/globals.css` |
| i18n (EN + AR) | 6 namespaces, full RTL flip, `dir` attribute bound to language | `client/src/locales/` + `client/src/lib/i18n.ts` |
| Stores | authStore (Zustand + persist), uiStore (theme sync) | `client/src/stores/` |
| API client | axios + 401→refresh interceptor + typed ApiError (RFC 7807) | `client/src/services/api/client.ts` |
| TanStack Query | queryClient + queryKeys factory covering all modules | `client/src/lib/queryClient.ts` |
| SignalR clients | Typed wrappers for 3 hubs + reconnect + JWT | `client/src/services/signalR/hubs.ts` |
| Route guards | RequireAuth + RequireRole (auto redirect to `/login?redirect=...`) | `client/src/routes/RequireAuth.tsx` |
| Routes | 40+ routes reachable via lazy chunks | `client/src/routes/router.tsx` |
| Home page | Full animated hero + pillars + CTA | `client/src/pages/public/Home.tsx` |
| Auth pages | Login, Register, ForgotPassword, ResetPassword, OnboardingWizard, SsoCallback | `client/src/pages/auth/` |
| Module page stubs | ~40 empty-state placeholders with owner + spec path | `client/src/pages/` |
| Stripe Elements | Reference checkout component for booking flows | `client/src/components/common/StripeCheckout.tsx` |
| Smoke tests | Vitest unit + Playwright E2E | `client/src/test/` |
| Build status | `npm run build` — 321 KB main (gzip 99 KB) | verified in CI |

### Dev + deploy
| Item | Status |
|---|---|
| Docker Compose (sqlserver + redis + mailhog) | ✅ `docker-compose.yml` |
| Multi-stage Dockerfile for API | ✅ `Dockerfile.api` |
| Node build + Nginx runtime Dockerfile for client | ✅ `Dockerfile.client` + `client/nginx.conf` |
| GitHub Actions CI (backend + client + security + status) | ✅ `.github/workflows/ci.yml` |
| Deploy workflow template (Azure App Service + Static Web Apps) | 🟡 `.github/workflows/deploy.yml` — commented until secrets are added |
| CODEOWNERS | ✅ Auto-routes PRs to the right reviewer |
| PR + issue templates | ✅ `.github/` |
| VS Code launch/tasks/settings | ✅ `.vscode/` |
| README with badges + architecture diagram + team table | ✅ `README.md` |
| Constitution + HANDOFF onboarding | ✅ |

### Repo meta
| Item | Status |
|---|---|
| Public repo | ✅ https://github.com/ma7moudalysalem/ScholarPath |
| 14 tracking issues (one per epic) | ✅ labeled with owner + iteration + priority |
| Labels (owner, epic, iteration, priority) | ✅ |
| Collaborator invitations sent to 4 team members | ✅ pending their acceptance |

---

## 4. What's next — week by week

### Iteration 1 (Week 1) — Auth + Profile + Admin shell
**Goal**: every teammate can log in, have a role, and see an admin dashboard shell.

- **@Madiha6776**: PB-001 Auth handlers (Register, Login, Refresh, Forgot/Reset, SSO, Onboarding, SwitchRole). PB-002 Profile CRUD.
- **@ma7moudalysalem**: PB-011 Admin shell (user search, onboarding queue, dashboard metrics card). Wire audit service into every mutation (PB-012 behavior).

### Iteration 2 (Week 2) — Discovery + Applications + Payments
**Goal**: student can search scholarships, apply (in-app or external), and Stripe is live in test mode.

- **@norra-mmhamed**: PB-003 SearchScholarshipsQuery + filters + Company listing CRUD. PB-004 Start → Draft → Submit → Track flow, single-active rule enforced in the handler. PB-013 Real Stripe.net calls replacing stub; webhook idempotency verified with replay tests.

### Iteration 3 (Week 3) — Consultant Booking + Company Review + Notifications + Profit Share
**Goal**: booking flow end-to-end with refund matrix, notifications reach users, profit share recorded on every capture.

- **@TasneemShaaban**: PB-006 Consultant Booking — availability CRUD, RefundCalculatorService (pure function, unit-test matrix), accept/reject/cancel flows. PB-014 Profit Share config UI + calculator.
- **@yousra-elnoby**: PB-005 Company Review + Rating flow (post-decision).
- **@Madiha6776**: PB-010 Notifications dispatcher wiring to every domain event; email templates.

### Iteration 4 (Week 4) — Community + AI + Resources + Audit
**Goal**: students can chat 1:1, ask the AI, and read articles. Audit log complete.

- **@yousra-elnoby**: PB-007 Community forum + 1:1 chat (SignalR handlers). PB-009 Resources Hub with Draft → PendingReview → Published state machine.
- **@ma7moudalysalem**: PB-008 AI features — swap StubAiService for OpenAI or Azure OpenAI; recommendations cached per user for 1h; eligibility checker per-criterion. PB-012 Audit `[Auditable]` pipeline behavior + 30-day delayed delete job.

### Freeze + defense prep (Week 5)
- Performance tuning (SQL Server full-text index test with 100K seeded rows)
- E2E test coverage pass
- Deploy to Azure staging
- Demo script + slides

---

## 5. How the work is tracked

- **GitHub Issues** — one epic per module (14 total), labeled by owner / iteration / priority. https://github.com/ma7moudalysalem/ScholarPath/issues
- **tasks.md inside each spec folder** — the per-module checklist, committed with the repo, so you can `git blame` progress.
- **CI badge** — every PR must pass before merge (backend + client lanes gate the merge; security is advisory).
- **CODEOWNERS** — auto-requests the right reviewer on every PR.
- **Weekly standups** — 15 min on Mondays; each person reports iteration progress + blockers.

---

## 6. Academic artifacts — where to find each

| Required artifact | Location |
|---|---|
| Software Requirements Specification (SRS) | `docs/SRS.md` — full 211 FRs + 203 use cases + 159 user stories |
| Use-case diagrams | Derivable from SRS §6 (203 use-case descriptions). For visualization, import into Enterprise Architect or draw.io. |
| **Entity Relationship Diagram (ERD)** | `docs/ERD.md` — Mermaid `erDiagram` rendered natively by GitHub |
| **ERD mapping** (tables, constraints, indexes) | `docs/ERD-MAPPING.md` — every entity → table → FK/unique index/filter |
| **Class diagrams** (UML) | `docs/CLASS-DIAGRAMS.md` — Mermaid `classDiagram` for Domain aggregates + Application CQRS + Infrastructure adapters |
| Architecture deep dive (layered + request lifecycle) | `docs/ARCHITECTURE.md` |
| Auth sequence diagrams | `docs/AUTH.md` |
| Payment flow diagrams + refund matrix | `docs/PAYMENTS.md` |
| Data model snapshot | The EF migration files under `server/src/ScholarPath.Infrastructure/Migrations/` are the authoritative schema. Run `dotnet ef migrations script` to export SQL. |
| Design system | `docs/DESIGN.md` |
| Testing strategy | `docs/TESTING.md` |
| Non-functional requirements | `docs/SRS.md` §5 |

---

## 7. Why this setup (for the grad defense panel)

- **Spec-driven**: every feature traces back to an SRS user story. Zero "scope creep" by construction.
- **Clean Architecture**: enforced by project references; violations fail the build. Defends against vendor lock-in.
- **Test-first**: CI gates every PR with coverage floors. Defends against regression-fear.
- **Bilingual from day one**: not an afterthought. Many platforms bolt on Arabic later and fail.
- **Gated with audit**: every mutation logged. Defends against compliance questions.
- **Stripe with idempotency**: webhook replay-safe. Defends against "what about failures" questions.
- **Role-aware with upgrade path**: Student can become Consultant without losing history. Defends against complex user-journey questions.
- **Observable**: Serilog + RFC 7807 + structured logs. Defends against "how do you debug production?" questions.

---

## 8. How to give feedback / propose a change

1. Open an issue with the `enhancement` label.
2. If it deviates from the SRS, propose a constitution amendment (PR to `.specify/memory/constitution.md`) with rationale.
3. If approved by 2-of-3 owner teammates, rebase your work on top.

---

*ScholarPath — graduation project, 2026.*
