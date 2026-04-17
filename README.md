<div align="center">

# 🎓 ScholarPath

**A gated, bilingual scholarship platform that connects students with verified consultants, scholarship providers, and a peer-learning community — in one personalized, AI-assisted workspace.**

[![Status](https://img.shields.io/badge/status-active%20development-0ea5e9?style=flat-square)](#)
[![Frontend](https://img.shields.io/badge/Frontend-React_19-149eca?style=flat-square&logo=react)](#)
[![Backend](https://img.shields.io/badge/Backend-.NET_10-512bd4?style=flat-square&logo=dotnet)](#)
[![Database](https://img.shields.io/badge/Database-SQL_Server_2022-cc2927?style=flat-square&logo=microsoftsqlserver)](#)
[![Payments](https://img.shields.io/badge/Payments-Stripe-635bff?style=flat-square&logo=stripe)](#)
[![Realtime](https://img.shields.io/badge/Realtime-SignalR-512bd4?style=flat-square)](#)
[![Style](https://img.shields.io/badge/UI-Tailwind_v4-06b6d4?style=flat-square&logo=tailwindcss)](#)
[![i18n](https://img.shields.io/badge/i18n-EN_%7C_AR_RTL-10b981?style=flat-square)](#)

[🚀 Quick start](#-quick-start) ·
[🏛 Architecture](#-architecture) ·
[👥 Team](#-team--module-ownership) ·
[🗺 Roadmap](#-roadmap-srs-iterations) ·
[🔒 Security](#-security-baseline) ·
[📚 Docs](./docs)

</div>

---

## 📌 What it does

ScholarPath is a **gated** platform (home page is the only public surface). Once registered, students can:

- **Discover** matching scholarships across 100K+ listings with AI-ranked match scores.
- **Apply** in-app or track external applications on a unified kanban.
- **Book** verified consultants with Stripe-held payments (capture-on-accept, auto-refund on expiry).
- **Chat** 1:1 in real time, join a moderated community forum, or search a curated resources hub.
- **Ask** an AI assistant about eligibility, essays, and study-abroad planning.

Scholarship providers publish listings and review applications; consultants publish availability and earn via Stripe Connect; admins oversee approvals, moderation, and financial configuration.

Full functional spec in [`docs/SRS.md`](./docs/SRS.md) — 211 functional requirements, 203 use cases, 159 user stories.

---

## 🧰 Stack

| Layer | Technology |
|---|---|
| **Backend** | .NET 10 · ASP.NET Core · EF Core 10 · MediatR 14 · FluentValidation 12 · AutoMapper 14 · Serilog · Scalar OpenAPI |
| **Database** | SQL Server 2022 |
| **Auth** | ASP.NET Core Identity · RS256 JWT · Refresh rotation · Google + Microsoft SSO |
| **Real-time** | SignalR (ChatHub · NotificationHub · CommunityHub) with Redis backplane ready |
| **Payments** | Stripe.net 51 (PaymentIntents · Connect · webhooks with signature verification) |
| **Background** | Hangfire (feature-flagged; 5 scheduled jobs stubbed) |
| **Frontend** | React 19 · Vite 8 · TypeScript 6 · Tailwind CSS v4 · shadcn/ui + Radix · Motion 12 · TanStack Query 5 · Zustand 5 · React Hook Form + Zod |
| **i18n** | i18next 26 · Full EN + AR with RTL direction flip |
| **DevX** | Docker Compose (SQL Server + Redis + MailHog) · GitHub Actions · Scalar API docs · Vitest + Playwright · xUnit + Testcontainers |

---

## 🚀 Quick start

> ≈ 10 minutes from `git clone` to login screen.

### Prerequisites
- .NET SDK **10.0.6+**
- Node.js **22+** (LTS)
- Docker Desktop (for SQL Server + Redis + MailHog)

### Boot the stack

```bash
git clone https://github.com/ma7moudalysalem/ScholarPath.git
cd ScholarPath
cp .env.example .env           # fill Stripe/OAuth test-mode keys when ready

docker compose up -d           # sqlserver + redis + mailhog

# Backend (new terminal)
cd server
dotnet ef database update --project src/ScholarPath.Infrastructure \
                          --startup-project src/ScholarPath.API
dotnet run --project src/ScholarPath.API
# → Scalar API docs: http://localhost:5000/scalar/v1
# → Health:           http://localhost:5000/health

# Frontend (new terminal)
cd client
npm ci
npm run dev
# → App: http://localhost:5173
```

### Seeded accounts (dev only)

| Role        | Email                              | Password       |
|-------------|------------------------------------|----------------|
| Admin       | `admin@scholarpath.local`          | `Admin123!`    |
| Student     | `student@scholarpath.local`        | `Student123!`  |
| Company     | `company@scholarpath.local`        | `Company123!`  |
| Consultant  | `consultant@scholarpath.local`     | `Consult123!`  |

### MailHog (dev email inbox)
Open http://localhost:8025 to see every email the app sends.

---

## 🏛 Architecture

Clean Architecture — dependencies flow inward only.

```
┌───────────────────────────────────────────────────────────────────────┐
│  client/    React 19 + Vite 8 + Tailwind v4 + shadcn/Radix + i18n    │
│  (static SPA · routing · layouts · stores · API + SignalR clients)    │
└──────────────────┬────────────────────────────────────────────────────┘
                   │ HTTPS · JWT bearer · WebSocket for SignalR
                   ▼
┌───────────────────────────────────────────────────────────────────────┐
│  server/ScholarPath.API    ASP.NET Core 10 host                       │
│  (controllers · middleware pipeline · hubs · webhooks · Scalar)       │
└──────────────────┬────────────────────────────────────────────────────┘
                   │ MediatR · IRequest → IRequestHandler
                   ▼
┌───────────────────────────────────────────────────────────────────────┐
│  ScholarPath.Application                                              │
│  (Commands · Queries · Validators · DTOs · Pipeline Behaviors)        │
│  14 module slices — one per backlog epic (PB-001 … PB-014)            │
└───┬───────────────────────────────────────────────────────────────┬───┘
    ▼                                                               ▼
┌─────────────────────────┐                 ┌─────────────────────────────┐
│  ScholarPath.Domain     │                 │  ScholarPath.Infrastructure │
│  40 entities · enums ·  │◀────implements──│  EF DbContext · Identity    │
│  events · interfaces    │                 │  Stripe · SignalR · Hangfire│
│  (pure, no deps)        │                 │  Email · Blob · AI · Audit  │
└─────────────────────────┘                 └──────────────┬──────────────┘
                                                           ▼
                                  ┌────────────────────────────────────────┐
                                  │ SQL Server 2022 · Redis · MailHog      │
                                  │ Stripe · OpenAI · Azure Blob / KeyVault│
                                  └────────────────────────────────────────┘
```

Full deep dive in [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md).

### Design system

- **Apple-inspired rhythm** — binary light/dark surfaces (`#ffffff` / `#f5f5f7` / `#000000`), -0.02em letter-spacing on headlines, 1.08 hero leading, 980px pill CTAs.
- **Typography** — Inter for Latin, IBM Plex Sans Arabic for Arabic, JetBrains Mono for code.
- **Motion** — 120/200/320/500ms durations with Apple easing `cubic-bezier(0.22, 1, 0.36, 1)`.
- **RTL-first** — `dir="rtl"` flows from i18n store; every layout uses logical CSS properties.
- **Dark mode** — via `[data-theme="dark"]` + system preference sync.

See [`docs/DESIGN.md`](./docs/DESIGN.md) for tokens, typography scale, shadow levels, and motion rules.

---

## 👥 Team & module ownership

| Module | Epic | Owner | SRS range | Iteration |
|---|---|---|---|---|
| Authentication, Access, Onboarding | PB-001 | [@Madiha6776](https://github.com/Madiha6776) | US-001..US-014 | 1 |
| Profile & Account Management | PB-002 | [@Madiha6776](https://github.com/Madiha6776) | US-015..US-019 | 1 |
| Scholarship Discovery & Listing | PB-003 | [@norra-mmhamed](https://github.com/norra-mmhamed) | US-020..US-030 | 2 |
| In-App Application & External Tracking | PB-004 | [@norra-mmhamed](https://github.com/norra-mmhamed) | US-031..US-044 | 2 |
| Company Review, Payment, Rating | PB-005 | [@yousra-elnoby](https://github.com/yousra-elnoby) | US-045..US-050 | 3 |
| Consultant Booking, Payment, Rating | PB-006 | [@TasneemShaaban](https://github.com/TasneemShaaban) | US-051..US-071 | 3 |
| Community + Chat | PB-007 | [@yousra-elnoby](https://github.com/yousra-elnoby) | US-072..US-099 | 4 |
| AI Features | PB-008 | [@ma7moudalysalem](https://github.com/ma7moudalysalem) | US-082..US-087 | 4 |
| Resources Hub | PB-009 | [@yousra-elnoby](https://github.com/yousra-elnoby) | US-088..US-102 | 4 |
| Notifications | PB-010 | [@Madiha6776](https://github.com/Madiha6776) | US-103..US-117 | 3 |
| Admin Portal | PB-011 | [@ma7moudalysalem](https://github.com/ma7moudalysalem) | US-118..US-138 | 1 |
| Audit & Compliance | PB-012 | [@ma7moudalysalem](https://github.com/ma7moudalysalem) | US-139..US-142 | 4 |
| Payment Processing & Settlement | PB-013 | [@norra-mmhamed](https://github.com/norra-mmhamed) | US-143..US-152 | 2 |
| Portal Profit Share | PB-014 | [@TasneemShaaban](https://github.com/TasneemShaaban) | US-153..US-159 | 3 |

**Team lead + architect + AI**: [@ma7moudalysalem](https://github.com/ma7moudalysalem) — owns shared infrastructure (`Program.cs`, `DbContext`, migrations, CI, design system) and reviews every PR.

CODEOWNERS auto-routes PRs to the right reviewer.

---

## 🗺 Roadmap (SRS iterations)

```
┌ Iteration 1 ─────────────────┐   ┌ Iteration 2 ─────────────────┐
│ PB-001 Auth + Onboarding     │   │ PB-003 Scholarships          │
│ PB-002 Profile               │ → │ PB-004 Applications          │
│ PB-011 Admin shell           │   │ PB-013 Payments              │
└──────────────────────────────┘   └──────────────────────────────┘
                                                │
                                                ▼
┌ Iteration 3 ─────────────────┐   ┌ Iteration 4 ─────────────────┐
│ PB-005 Company Review        │   │ PB-007 Community + Chat      │
│ PB-006 Consultant Booking    │ → │ PB-008 AI                    │
│ PB-010 Notifications         │   │ PB-009 Resources             │
│ PB-014 Profit Share          │   │ PB-012 Audit & Compliance    │
└──────────────────────────────┘   └──────────────────────────────┘
```

Follow live progress in the [14 tracking issues](https://github.com/ma7moudalysalem/ScholarPath/issues).

---

## 🔒 Security baseline

- **Passwords** hashed with PBKDF2-HMAC-SHA256 (ASP.NET Core Identity default).
- **JWT** signed with RS256 asymmetric keys from Azure Key Vault in prod; HMAC fallback in dev.
- **Lockout** after 5 consecutive failed logins in 15 min; refresh-token rotation with replacement chain prevents replay.
- **TLS 1.2+** enforced; HSTS + COOP + CORP + Permissions-Policy headers on every response.
- **Stripe webhooks** verified by signature + stored with unique `event.id` (idempotency).
- **RFC 7807 Problem Details** on every error; no stack traces in production.
- **Rate limiting** (10 req/min per-IP) on `/api/auth/*`.
- **OWASP Top 10** mitigations documented in [`docs/AUTH.md`](./docs/AUTH.md).
- **No secrets in code** — CI runs Gitleaks + Trivy on every push.

---

## 🧪 Testing

- **Backend**: xUnit + FluentAssertions + NSubstitute · integration via `WebApplicationFactory` + Testcontainers-MsSql.
- **Frontend**: Vitest + Testing Library (happy-dom) · Playwright for Chromium + Firefox + mobile.
- **Coverage target**: ≥70% across unit + integration.
- **CI gates every PR**: backend build + test + format check; frontend lint + typecheck + test + build; Trivy + Gitleaks security scan.

```bash
cd server && dotnet test               # 8/8 smoke tests passing
cd client && npm run typecheck         # clean
cd client && npm run lint              # clean
cd client && npm run test              # Vitest unit green
cd client && npm run test:e2e          # Playwright smoke green
```

Patterns + examples in [`docs/TESTING.md`](./docs/TESTING.md).

---

## 📚 Documentation

| Doc | What it covers |
|---|---|
| [ARCHITECTURE.md](./docs/ARCHITECTURE.md) | Layers · request lifecycle · event flow · state machines · data invariants |
| [AUTH.md](./docs/AUTH.md) | Register · login · SSO · refresh · role switcher · onboarding |
| [PAYMENTS.md](./docs/PAYMENTS.md) | Stripe lifecycle · refund matrix · webhook idempotency · Connect |
| [RTL.md](./docs/RTL.md) | Arabic + logical CSS properties + testing parity |
| [TESTING.md](./docs/TESTING.md) | Testing pyramid · examples · coverage |
| [DESIGN.md](./docs/DESIGN.md) | Design system tokens + components |
| [CHROME-DEVTOOLS-MCP.md](./docs/CHROME-DEVTOOLS-MCP.md) | Visual QA with a headless Chrome |
| [HANDOFF.md](./docs/HANDOFF.md) | Onboarding guide for teammates joining a module |
| [SRS.md](./docs/SRS.md) | Full Software Requirements Specification |

---

## 🤝 Contributing

See [`CONTRIBUTING.md`](./CONTRIBUTING.md). Every PR must:

- Reference an **FR-xxx** or **US-xxx** from the SRS.
- Pass **all CI gates** (build · test · typecheck · lint · format · security scan).
- Include **EN + AR** translations for any user-facing strings.
- Touch only files inside the module's **owner scope** (or request cross-owner review).

Branch naming: `feat/PB-xxx-short-slug` · `fix/PB-xxx-bug-slug` · `chore/description`.
Commit format: [Conventional Commits](https://www.conventionalcommits.org/).

---

## 📜 License

See [`LICENSE`](./LICENSE).

<div align="center">

*ScholarPath — graduation project, 2026.*

</div>
