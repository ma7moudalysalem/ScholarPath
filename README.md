<div align="center">

# 🎓 ScholarPath

**A gated, bilingual (English · Arabic) scholarship platform that takes a student from discovery to a decision, with verified providers and mentors and explainable AI advice.**

[![Backend](https://img.shields.io/badge/Backend-.NET_10-512bd4?style=flat-square&logo=dotnet)](#-stack)
[![Frontend](https://img.shields.io/badge/Frontend-React_19-149eca?style=flat-square&logo=react)](#-stack)
[![Database](https://img.shields.io/badge/Database-SQL_Server-cc2927?style=flat-square&logo=microsoftsqlserver)](#-stack)
[![Realtime](https://img.shields.io/badge/Realtime-SignalR-512bd4?style=flat-square)](#-stack)
[![AI](https://img.shields.io/badge/AI-RAG_·_explainable_rules-0ea5e9?style=flat-square)](#-the-ai-features)
[![i18n](https://img.shields.io/badge/i18n-EN_%7C_AR_RTL-10b981?style=flat-square)](#-stack)
[![License](https://img.shields.io/badge/license-MIT-lightgrey?style=flat-square)](./LICENSE)

[What it does](#-what-it-does) ·
[AI features](#-the-ai-features) ·
[Evaluation](#-evaluation-and-research) ·
[Quick start](#-quick-start) ·
[Architecture](#-architecture) ·
[Team](#-team) ·
[Docs](#-documentation)

</div>

---

## 📌 What it does

Students look for scholarships across listing sites, provider pages, social media groups and their own spreadsheets, and none of these verifies who offers help or follows an application to a decision. ScholarPath puts the whole process in one **gated** environment: only the home, authentication and information pages are public, and every other route requires an account and a role.

- **Students** discover listings, check their eligibility, keep documents in a reusable vault, apply in-app or track external applications, book verified consultants and join a moderated community.
- **Scholarship providers** publish listings and review applications; a rejection must carry a reason.
- **Consultants** publish availability and run video sessions under published cancellation and no-show rules.
- **Administrators** approve providers and consultants before they can publish or mentor, moderate content and adjudicate disputes.

The platform runs in **free-access mode**: the payment flows are implemented and tested but switched off by configuration, and every payment screen is hidden.

The full specification is in [`docs/SRS.md`](./docs/SRS.md) and [`docs/srs/ScholarPath-SRS.docx`](./docs/srs/ScholarPath-SRS.docx).

---

## 🤖 The AI features

The AI advises; it never decides.

| Feature | How it works |
|---|---|
| **Recommendations** | A content-based ranking by academic level, field and destination, gated by level and nationality. Deterministic: the same profile always gives the same ranking, and every match carries the criteria that produced it. |
| **Eligibility check** | Compares the profile with each listing criterion (level, destination, nationality, field) and reports *met*, *partially met*, *not met* or *unknown*; a missing profile field gives *unknown*, never *not eligible*. |
| **What would open more listings** | Counterfactual suggestions: the single change (a destination to add, a level to reach) that would make the most listings eligible. |
| **Assistant (RAG)** | Answers from retrieved platform documents only. Retrieval fuses BM25 over English and Arabic text with embeddings by reciprocal rank; with no model configured it answers extractively, offline. |

Recommendation scoring and eligibility contain no model, so they behave identically under every AI provider (offline, OpenAI or Azure OpenAI).

---

## 📊 Evaluation and research

The evaluation harness ships with the code in [`server/tools/ScholarPath.Eval`](./server/tools/ScholarPath.Eval), and the figures in the accompanying paper are reproducible from tag [`paper-eval-v4`](https://github.com/ma7moudalysalem/ScholarPath/tree/paper-eval-v4).

- **Data:** 1,712 student profiles calibrated to UNESCO UIS statistics for twelve Arab countries, and 528 real listings from official sources (DAAD, Erasmus Mundus and 29 government and university programmes).
- **Recommendations:** measured for every student against an exhaustive ceiling, with bootstrap confidence intervals and Holm-adjusted tests ([`stats.py`](./server/tools/ScholarPath.Eval/stats.py)).
- **External check:** the eligibility verdicts agree with DAAD's own published criteria on all 39,300 compared pairs ([`real/external_check.py`](./server/tools/ScholarPath.Eval/real/external_check.py)).
- **Retrieval and answers:** known-item retrieval in English and Arabic, and assistant answers judged by two independent models.
- **Benchmark:** a bilingual eligibility question set of 1,320 items in English and Arabic ([`benchmark/`](./server/tools/ScholarPath.Eval/benchmark)).

```bash
cd server/tools/ScholarPath.Eval
dotnet run -- --mode recommendation --students 1712 --oracle 1712 --as-of 2026-10-27 --connection "<database>"
```

---

## 🧰 Stack

| Layer | Technology |
|---|---|
| **Backend** | .NET 10 · ASP.NET Core · EF Core · MediatR (CQRS) · FluentValidation · Serilog · Scalar OpenAPI |
| **Database** | SQL Server (Azure SQL in production) |
| **Auth** | ASP.NET Core Identity · RS256 JWT with refresh rotation · Google and Microsoft SSO |
| **Real-time** | SignalR (chat, notifications, community) with an optional Redis backplane |
| **Background jobs** | Hangfire: booking expiry, session reminders, no-show detection, deadline closure |
| **AI** | Offline BM25 + extractive answers · OpenAI · Azure OpenAI (chat and embeddings) |
| **Frontend** | React 19 · Vite · TypeScript · Tailwind CSS v4 · Radix UI · TanStack Query · Zustand · React Hook Form + Zod |
| **i18n** | i18next · English and Arabic with a right-to-left layout |
| **Cloud** | Azure App Service · Azure SQL · Blob Storage · Key Vault · Communication Services (video) · Static Web Apps |
| **DevX** | Docker Compose · GitHub Actions · xUnit · Vitest · Playwright |

---

## 🚀 Quick start

### Prerequisites
- .NET SDK 10
- Node.js 22+
- Docker Desktop (SQL Server, Redis, MailHog)

```bash
git clone https://github.com/ma7moudalysalem/ScholarPath.git
cd ScholarPath
cp .env.example .env

docker compose up -d           # SQL Server + Redis + MailHog

# Backend
cd server
dotnet ef database update --project src/ScholarPath.Infrastructure --startup-project src/ScholarPath.API
dotnet run --project src/ScholarPath.API
# API docs: http://localhost:5000/scalar/v1 · Health: http://localhost:5000/health

# Frontend (second terminal)
cd client
npm ci
npm run dev
# App: http://localhost:5173
```

The development database is seeded with one account per role (see `server/src/ScholarPath.Infrastructure/Persistence/Seed`). MailHog at http://localhost:8025 shows every email the app sends.

---

## 🏛 Architecture

A modular monolith in four layers (Domain, Application, Infrastructure, API), with dependencies pointing inward only.

```
client/   React 19 SPA ─── HTTPS · JWT · WebSocket (SignalR)
              │
              ▼
ScholarPath.API             controllers · middleware · hubs
              │  MediatR
              ▼
ScholarPath.Application     commands · queries · validators · pipeline behaviours
              │
     ┌────────┴─────────┐
     ▼                  ▼
ScholarPath.Domain   ScholarPath.Infrastructure
entities · events    EF Core · Identity · SignalR · Hangfire · AI · Blob · email
```

Applications, bookings and listings change state only along the transitions the server enforces. See [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md).

---

## 👥 Team

| Module | Epic | Owner |
|---|---|---|
| Authentication, Access, Onboarding | PB-001 | [@Madiha6776](https://github.com/Madiha6776) |
| Profile & Account Management | PB-002 | [@Madiha6776](https://github.com/Madiha6776) |
| Scholarship Discovery & Listing | PB-003 | [@norra-mmhamed](https://github.com/norra-mmhamed) |
| In-App Application & External Tracking | PB-004 | [@norra-mmhamed](https://github.com/norra-mmhamed) |
| Provider Review & Rating | PB-005 | [@yousra-elnoby](https://github.com/yousra-elnoby) |
| Consultant Booking & Rating | PB-006 | [@TasneemShaaban](https://github.com/TasneemShaaban) |
| Community + Chat | PB-007 | [@yousra-elnoby](https://github.com/yousra-elnoby) |
| AI Features | PB-008 | [@ma7moudalysalem](https://github.com/ma7moudalysalem) |
| Resources Hub | PB-009 | [@yousra-elnoby](https://github.com/yousra-elnoby) |
| Notifications | PB-010 | [@Madiha6776](https://github.com/Madiha6776) |
| Admin Portal | PB-011 | [@ma7moudalysalem](https://github.com/ma7moudalysalem) |
| Audit & Compliance | PB-012 | [@ma7moudalysalem](https://github.com/ma7moudalysalem) |
| Payment Processing & Settlement | PB-013 | [@norra-mmhamed](https://github.com/norra-mmhamed) |
| Portal Profit Share | PB-014 | [@TasneemShaaban](https://github.com/TasneemShaaban) |

**Team lead and architect:** [@ma7moudalysalem](https://github.com/ma7moudalysalem), owner of the shared infrastructure, the AI features and the evaluation.

Supervised by Dr. Nadia Abd-Alsabour, Cairo University.

---

## 🔒 Security

- ASP.NET Core Identity password hashing, lockout, and refresh-token rotation with a replacement chain.
- Role checks on every route, enforced by the server from the active-role claim.
- RFC 9457 problem details on every error; no stack traces outside development.
- Rate limits on the authentication and AI endpoints.
- Secrets read from Azure Key Vault in production; nothing secret in the repository.
- Personal data (emails, card and phone numbers) removed from questions before they reach a model.

---

## 🧪 Testing

```bash
cd server && dotnet test        # unit and integration tests
cd client && npm run typecheck && npm run lint && npm run test
```

CI builds and tests the server and the client on every pull request. See [`docs/TESTING.md`](./docs/TESTING.md).

---

## 📚 Documentation

| Doc | What it covers |
|---|---|
| [ARCHITECTURE.md](./docs/ARCHITECTURE.md) | Layers, request lifecycle, events, state machines |
| [AUTH.md](./docs/AUTH.md) | Registration, login, SSO, refresh, roles, onboarding |
| [ERD.md](./docs/ERD.md) · [CLASS-DIAGRAMS.md](./docs/CLASS-DIAGRAMS.md) | Data model and class diagrams |
| [PAYMENTS.md](./docs/PAYMENTS.md) | The payment design (disabled in free-access mode) |
| [RTL.md](./docs/RTL.md) | Arabic and right-to-left layout |
| [TESTING.md](./docs/TESTING.md) | Testing approach |
| [DESIGN.md](./docs/DESIGN.md) | Design system |
| [deployment.md](./docs/deployment.md) | Azure deployment |
| [SRS.md](./docs/SRS.md) | Software Requirements Specification |

---

## 📜 License

[MIT](./LICENSE)

<div align="center">

*ScholarPath — graduation project, Cairo University, 2026.*

</div>
