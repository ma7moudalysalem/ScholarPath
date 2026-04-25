# ScholarPath Constitution

_The binding principles for every contribution to ScholarPath v2._

## Core Principles

### I. Gated-First Platform
The Home Page is the only public route. Every other page, API endpoint, asset, and socket connection requires an authenticated session. Guests attempting to access protected routes are redirected to login/register with the original destination preserved. The gating check is enforced at three layers: (1) frontend route guards, (2) backend `[Authorize]` attributes, (3) SignalR hub authorization handlers. This is non-negotiable.

### II. Clean Architecture with Strict Dependency Flow
Dependencies flow inward only: `API -> Application -> Domain` and `Infrastructure -> Application -> Domain`. The Domain layer has zero external dependencies. The Application layer depends only on Domain abstractions and MediatR. Infrastructure implements Application interfaces. Violations fail the build (enforced via `Directory.Build.props` restrictions). Business logic lives in Application handlers; controllers are thin pass-throughs.

### III. SRS Traceability (NON-NEGOTIABLE)
Every pull request title, commit message, and spec artifact must reference at least one Functional Requirement (FR-xxx) or User Story (US-xxx) from the SRS. The traceability matrix in `docs/srs-reference.md` is the source of truth. Features not in the SRS require a constitution amendment before implementation.

### IV. Bilingual Parity (EN + AR RTL)
Every shipped feature must have complete English and Arabic translations with correct RTL direction, logical CSS properties (`margin-inline-start`, `inset-block`, etc.), and locale-specific formatting (dates, numbers, currency). An English-only page or an untranslated label is a shipping blocker. The `dir="rtl"` attribute flows from the i18n store through `<html>` to every layout component.

### V. Test-First Discipline (>=70% coverage)
Unit tests accompany every Application handler. Integration tests cover every API endpoint and SignalR hub. A smoke E2E test validates each critical user flow (register, login, apply, book, chat). No PR merges without green CI. The 70% coverage floor is enforced via `coverlet` + threshold check in the CI workflow.

### VI. Security Baseline
Passwords hashed with PBKDF2-HMAC-SHA256 (ASP.NET Core Identity default). JWT signed with RS256 asymmetric keys from Azure Key Vault (local dev uses `secrets.json`). TLS 1.2+ for all traffic. Stripe webhooks verified via signature. File uploads antivirus-scanned before persistence. OWASP Top 10 mitigations documented. Secrets never committed to git; secret-scanning runs in CI.

### VII. Observability
Every request produces a structured Serilog log entry with correlation ID, user ID, route, status, and duration. Every MediatR handler logs its execution. Every Stripe webhook event is stored with its raw payload and processing outcome. Failures produce RFC 7807 Problem Details responses. Hangfire dashboard is the ops console (Admin-only).

### VIII. No Teammate Blocked
Before any teammate writes their first handler, their module must already have: a populated `spec.md` (user stories + acceptance criteria), a `plan.md` (architecture touchpoints + entities + endpoints), a `tasks.md` (ordered checklist with owner tag), page skeletons in the frontend, empty vertical-slice folders in the backend, localized EN+AR keys, and a route that renders an empty-state placeholder. A module without all seven is blocked until it has all seven.

## Technology Stack (Non-Negotiable Versions)

Backend targets **.NET 10.0.6 LTS**, **EF Core 10.0.6**, **MediatR 14.1.0**, **AutoMapper 14** (free MIT - NOT v15 commercial), **FluentValidation 12.1.1**, **Serilog 4.3**, **Scalar.AspNetCore 2.14**, **Stripe.net 51.0.0**, **Hangfire 1.8.23**, **Testcontainers + xUnit + FluentAssertions + NSubstitute**.

Frontend targets **React 19.2.5**, **Vite 8.0.8**, **TypeScript 6.0.3**, **Tailwind CSS 4.2.2** (with `@theme` directive - no `tailwind.config.js` tokens), **shadcn/ui 4.3** on **Radix UI**, **Motion 12.38** (from `"motion/react"`), **TanStack Query 5.99**, **Zustand 5.0.12**, **React Hook Form 7.72 + Zod 4.3**, **i18next 26 + react-i18next 17**, **react-router 7**, **date-fns 4.1**, **Vitest + Playwright**.

Database is **SQL Server 2022**. Dev dependencies run via `docker-compose.yml` (sqlserver, redis, mailhog). Redis and Hangfire are feature-flagged off by default so teammates don't need them locally.

Version upgrades require a constitution amendment. Renovate is configured to propose, never auto-merge, major upgrades.

## Development Workflow and Quality Gates

Branch naming: `feat/PB-xxx-short-slug`, `fix/PB-xxx-short-slug`, `chore/description`, `docs/description`. Commit format: Conventional Commits (`feat(auth): ...`, `fix(bookings): ...`). Every PR must reference its PB-xxx epic and FR-xxx or US-xxx.

CI gates every PR: `dotnet build --no-warn` + `dotnet test` + `dotnet format --verify-no-changes` + `npm run lint` + `npm run typecheck` + `npm run test` + `npm run build` + Trivy security scan + secret detection. No exceptions.

Module ownership (current, post-rebalance 2026-05-10): **Mimi** owns PB-001 (Auth), PB-002 (Profile), PB-007 (Community+Chat), PB-009 (Resources), PB-010 (Notifications). **Nora** owns PB-003 (Scholarships), PB-004 (Applications), PB-013 (Payments), PB-014 (Profit Share). **Yosra** owns PB-005 (Company Review) and the moderate data-engineering stories in PB-016 (CDC, Silver dbt, DQ assertions, docs site). **Tasneem** owns PB-006 (Consultant Booking) and PB-015 (Power BI Analytics) plus the Power-BI-adjacent slivers of PB-016 + PB-018. **Mahmoud** owns PB-008 (AI), PB-011 (Admin), PB-012 (Audit), PB-016 lead (ADF + Gold + IaC), PB-017 (AI Economy), PB-018 (Real-time), and INFRA. Shared modules require two approvals (at least one module owner).

Cross-module changes require approval from all affected module owners. The `.github/CODEOWNERS` file enforces this automatically.

## Governance

This constitution supersedes ad-hoc decisions. When rules conflict, the constitution wins. Amendments require: (1) a PR to `.specify/memory/constitution.md`, (2) a documented rationale in the PR body linking to SRS or project goals, (3) approval from at least two of the three module-owner teammates, (4) a `Last Amended` date bump.

Every PR reviewer must verify constitution compliance and reject PRs that violate it. Complexity must be justified; abstractions not mandated by the constitution require explicit approval.

Runtime guidance for AI agents and contributors lives in `docs/ARCHITECTURE.md`, `docs/AUTH.md`, `docs/PAYMENTS.md`, `docs/RTL.md`, `docs/TESTING.md`.

## Analytics Principles (Part V, adopted 2026-05-11)

Analytics work (PB-015 through PB-018) layers on top of the transactional platform. These rules apply only to the analytics layer and do not relax any constraint from sections above.

**Principle 9 — PII never crosses the analytics boundary raw.** All data landing in Bronze is either already redacted (for AI prompts via PB-008's `RedactPii`) or already aggregated (for analytics-only entities). Gold must additionally hash any user identifier shown in public or role-restricted reports. Raw email, phone, payment tokens, and government IDs are forbidden in any Parquet file, Synapse external table, or Power BI dataset.

**Principle 10 — Row-Level Security is non-negotiable.** Every Power BI dataset carries a named RLS role per JWT `activeRole`. Admins, Finance, Consultant, and Student see strictly their own scope. An RLS regression is a sev-2 incident.

**Principle 11 — Analytics never blocks the OLTP.** Once PB-016 is in place, DirectQuery is turned off. Every dashboard reads from Synapse Serverless on Gold. Analytics refresh jobs must not generate new load against the live SQL Server during business hours.

**Principle 12 — Cost visibility is a first-class feature.** Every AI call carries a per-user cost. The `AiCostGate` rolling 24h cap enforces the limit; the PB-017 dashboard makes the trend visible; the PB-017 email alert surfaces users approaching the cap. Hidden AI spend is a policy violation.

**Version**: 1.1.0 | **Ratified**: 2026-04-17 | **Last Amended**: 2026-05-11 (Principles 9-12 added for the analytics track)
