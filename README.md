# ScholarPath

> **Gated scholarship platform** — a single, authenticated place for students to discover scholarships, apply in-app or track external applications, book verified Consultants, join a community, and consume AI-curated resources. Built for EN + AR RTL from day 1.

| | |
|---|---|
| **Status** | In active development — graduation project |
| **Stack** | .NET 10 · EF Core · SQL Server · React 19 · Tailwind v4 · shadcn/ui · Stripe · SignalR |
| **Method** | Spec-driven development |

## Quick start (≈ 10 min)

### Prerequisites
- .NET SDK 10.0.6+
- Node.js 22+ (LTS)
- Docker Desktop (for SQL Server + Redis + MailHog)

### Boot the stack

```bash
git clone https://github.com/ma7moudalysalem/ScholarPath.git
cd ScholarPath
cp .env.example .env    # fill placeholders with your test-mode keys
docker compose up -d    # sqlserver + redis + mailhog

# Backend
cd server
dotnet ef database update --project src/ScholarPath.Infrastructure --startup-project src/ScholarPath.API
dotnet run --project src/ScholarPath.API
# Scalar API docs: http://localhost:5000/scalar/v1

# Frontend (new terminal)
cd client
npm ci
npm run dev
# App: http://localhost:5173
```

### Log in with seeded accounts
| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@scholarpath.local` | `Admin123!` |
| Student | `student@scholarpath.local` | `Student123!` |
| Company | `company@scholarpath.local` | `Company123!` |
| Consultant | `consultant@scholarpath.local` | `Consult123!` |

## Architecture

- **Clean Architecture** (4 projects): `Domain` → `Application` → `Infrastructure` → `API`
- **CQRS via MediatR 14**: commands / queries / validators / DTOs per module
- **14 modules** (PB-001 … PB-014) — see `.specify/specs/README.md`
- **Real-time**: SignalR hubs (`ChatHub`, `NotificationHub`, `CommunityHub`)
- **Payments**: Stripe PaymentIntents + Connect + webhook signature verification
- **Background jobs**: Hangfire (feature-flagged off by default in dev)
- **Auth**: ASP.NET Core Identity + RS256 JWT + Google + Microsoft SSO
- **Frontend**: React 19 + Vite 8 + TypeScript 6 + Tailwind v4 + shadcn/ui + Zustand + TanStack Query
- **i18n**: EN + AR with full RTL support from day 1

Deep dives:
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — layers, DI, data flow
- [`docs/AUTH.md`](docs/AUTH.md) — sequence diagrams for register / login / SSO / refresh
- [`docs/PAYMENTS.md`](docs/PAYMENTS.md) — Stripe flow + refund matrix
- [`docs/RTL.md`](docs/RTL.md) — Arabic/RTL conventions + gotchas
- [`docs/TESTING.md`](docs/TESTING.md) — how we test, coverage targets
- [`docs/DESIGN.md`](docs/DESIGN.md) — design system + tokens
- [`docs/CHROME-DEVTOOLS-MCP.md`](docs/CHROME-DEVTOOLS-MCP.md) — visual QA setup

## Team & module ownership

| Module | Owner | Range |
|--------|-------|-------|
| PB-001 Auth + Onboarding | @Madiha6776 | US-001..US-014 |
| PB-002 Profile | @Madiha6776 | US-015..US-019 |
| PB-003 Scholarships | @norra-mmhamed | US-020..US-030 |
| PB-004 Applications | @norra-mmhamed | US-031..US-044 |
| PB-005 Company Review + Rating | @yousra-elnoby | US-045..US-050 |
| PB-006 Consultant Booking | @TasneemShaaban | US-051..US-071 |
| PB-007 Community + Chat | @yousra-elnoby | US-072..US-081, US-095..US-099 |
| PB-008 AI Features | @ma7moudalysalem | US-082..US-087 |
| PB-009 Resources Hub | @yousra-elnoby | US-088..US-094, US-100..US-102 |
| PB-010 Notifications | @Madiha6776 | US-103..US-117 |
| PB-011 Admin Portal | @ma7moudalysalem | US-118..US-138 |
| PB-012 Audit & Compliance | @ma7moudalysalem | US-139..US-142 |
| PB-013 Payments | @norra-mmhamed | US-143..US-152 |
| PB-014 Profit Share | @TasneemShaaban | US-153..US-159 |

**Team lead + architect**: @ma7moudalysalem (reviews shared infrastructure, Program.cs, DbContext, migrations, CI, design system).

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md). Every PR must:
- Reference an FR-xxx or US-xxx from the SRS
- Pass all CI gates (build, test, typecheck, lint, format, security scan)
- Carry EN + AR translations for any user-facing strings
- Touch only files inside your module's owner scope (or request cross-owner review)

## License

See [`LICENSE`](LICENSE).
