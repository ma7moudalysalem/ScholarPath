# ScholarPath — Professional Technical Audit Report

**Date:** 2026-03-11
**Auditor Role:** Principal Software Engineer, Security Auditor, Product Architect, QA Lead, UX Expert, Performance Engineer
**Scope:** Full codebase due diligence — 23 phases

---

## Phase 1 — System Overview

**ScholarPath** is an AI-powered scholarship discovery and community platform built as a full-stack web application.

| Layer | Technology | Version |
|-------|-----------|---------|
| Frontend | React + TypeScript | 19.0 / 5.7 |
| UI Library | MUI (Material UI) | 6.4 |
| State | Zustand (client) + TanStack Query (server) | 5.0 / 5.62 |
| Backend | ASP.NET Core Web API | .NET 10 |
| Architecture | Clean Architecture + CQRS (MediatR) | — |
| Database | SQL Server + EF Core | 10.0.3 |
| Caching | Redis (StackExchange.Redis) | 2.11.3 |
| Background Jobs | Hangfire | 1.8.23 |
| Auth | JWT + Google/Microsoft OAuth | — |
| Email | SendGrid / SMTP | 9.29.3 |
| i18n | i18next (EN/AR with RTL) | 24.2 |
| CI/CD | GitHub Actions | 3 workflows |

**Codebase Size:** ~173 C# files (server), ~67 TypeScript files (client), 4 database migrations, 2 test projects.

**Key Features Implemented:**
- User registration/login with JWT + OAuth (Google, Microsoft)
- Role-based onboarding (Student/Consultant/Company/Admin)
- Scholarship search, filtering, saving, and recommendations
- Application tracker with Kanban board
- Dashboard with status tiles, deadlines, and actions
- Admin panel for upgrade request approval
- Bilingual support (English/Arabic with RTL)
- Deadline reminder background jobs (Hangfire)

---

## Phase 2 — Architecture Map

```
┌─────────────────────────────────────────────────────┐
│                    Client (React 19)                 │
│  Pages → Components → Hooks → Services → Axios      │
│  Stores (Zustand) ←→ TanStack Query                 │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP/JSON (JWT Bearer)
┌──────────────────────▼──────────────────────────────┐
│              API Layer (ASP.NET Core)                │
│  Controllers → MediatR → Commands/Queries            │
│  Middleware: Exception, Security Headers, Rate Limit │
├─────────────────────────────────────────────────────┤
│            Application Layer (CQRS)                  │
│  Command Handlers, Query Handlers, Validators        │
│  AutoMapper, FluentValidation, Pipeline Behaviors    │
├─────────────────────────────────────────────────────┤
│              Domain Layer (Entities)                 │
│  23 Entities, 14 Enums, Interfaces                   │
├─────────────────────────────────────────────────────┤
│          Infrastructure Layer (Services)             │
│  EF Core DbContext, Repositories, TokenService       │
│  EmailService, CachingService, Hangfire Jobs         │
└──────────────────────┬──────────────────────────────┘
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
     SQL Server      Redis      SendGrid
```

**Architectural Issues Identified:**

| # | Issue | Severity |
|---|-------|----------|
| A1 | Controllers inject concrete `ApplicationDbContext` instead of `IApplicationDbContext` — violates dependency inversion | HIGH |
| A2 | `AuthController.CompleteOnboarding` has 57 lines of business logic that belongs in a command handler | HIGH |
| A3 | 6 dead entities (Group, GroupMember, Post, Comment, Like, Message) with DbSets, configurations, and migrations but zero implementation | HIGH |
| A4 | JSON columns used instead of proper relations (Tags, EligibleCountries, ChecklistJson, RemindersJson) — breaks queryability | HIGH |
| A5 | `IApplicationDbContext` is incomplete and bypassed by controllers — interface segregation violation | MEDIUM |

---

## Phase 3 — Functional Audit

| Feature | Status | Issues |
|---------|--------|--------|
| Registration | Working | Account enumeration risk (409 on duplicate email) |
| Login | Working | Generic error messages (correct) |
| OAuth (Google) | Working | Token validation via HTTP call, no signature check |
| OAuth (Microsoft) | Stubbed | No ClientId configured |
| Onboarding | Working | No error feedback to user on failure |
| Dashboard | Working | No error state, unbounded deadline query |
| Scholarship Search | Working | `LIKE '%search%'` — no full-text search |
| Scholarship Detail | Working | View count race condition |
| Scholarship Save | Working | Race condition on concurrent saves |
| Application Tracker | Working | No status transition validation |
| Kanban Board | Working | 200-item hard limit, no virtualization |
| Admin Upgrade Review | Working | No request title in decision dialog |
| File Upload | Working | Proper validation (type, size, count) |
| Community | **NOT IMPLEMENTED** | Placeholder "Coming Soon" page |
| Notifications | Partial | Entity exists, limited UI |
| Profile Edit | Partial | Page exists, limited functionality |
| Password Reset | **NOT IMPLEMENTED** | Endpoints exist but flow untested |

**Broken/Incomplete Workflows:**
1. **Rejected upgrade → permanent lockout**: User with `AccountStatus.Rejected` cannot log in or resubmit
2. **Community section** is a dead end with active navigation link
3. **Onboarding** silently fails with no user feedback on error

---

## Phase 4 — Business Logic Validation

| # | Issue | Severity |
|---|-------|----------|
| B1 | **No status transition validation** — Application can go from Rejected→Applied, Accepted→Planned freely | CRITICAL |
| B2 | **Race condition on upgrade requests** — Two simultaneous submissions can both pass the `AnyAsync` check | CRITICAL |
| B3 | **Race condition on scholarship save** — Check-then-act allows duplicate `SavedScholarship` rows | HIGH |
| B4 | **Race condition on application tracking** — Same pattern, duplicate `ApplicationTracker` rows possible | HIGH |
| B5 | **No concurrency control on status updates** — Two concurrent updates overwrite each other | MEDIUM |
| B6 | **Rejected users permanently locked out** — No path to resubmit or re-login | MEDIUM |
| B7 | **Fire-and-forget emails swallow exceptions** — `catch (Exception) { }` with no logging | MEDIUM |

---

## Phase 5 — Web Application Deep Audit

| Area | Finding | Severity |
|------|---------|----------|
| UI Responsiveness | Kanban loads 200 items without virtualization | MEDIUM |
| Thread Safety | Client-side `isRefreshing` module variable — multi-tab race condition | HIGH |
| Background Tasks | Hangfire disabled by default; DeadlineReminderJob has TODO for notification integration | MEDIUM |
| Memory | No observable memory leaks; React cleanup patterns correct | OK |
| Network Interruption | No offline support, no retry logic, no timeout-specific error messages | MEDIUM |
| Error Recovery | Session expiry clears state and loses user's current page | MEDIUM |

---

## Phase 6 — UI/UX Review

| # | Issue | Severity |
|---|-------|----------|
| U1 | **Delete from TrackerCard has no confirmation dialog** (CardDetailDrawer has one, list view does not) | HIGH |
| U2 | **Hover-only actions on TrackerCard** — inaccessible on mobile/touch/keyboard | HIGH |
| U3 | Missing ARIA labels on form fields in CardDetailDrawer | MEDIUM |
| U4 | No loading indicators during mutations (status change, notes save, checklist toggle) | MEDIUM |
| U5 | Empty Kanban columns show only `--` instead of a helpful empty state | MEDIUM |
| U6 | Color contrast issue on status chips in dark mode (gray #757575 + white text) | MEDIUM |
| U7 | CardDetailDrawer 450px too wide for mobile landscape orientation | MEDIUM |
| U8 | Date picker not tested for RTL layout correctness | MEDIUM |
| U9 | No undo mechanism for accidental reminder toggles | LOW |

---

## Phase 7 — Localization Review (Arabic / English)

| Area | Status | Issues |
|------|--------|--------|
| Translation keys | **COMPLETE** — All 438 keys present in both `en.json` and `ar.json` | OK |
| RTL layout | Mostly working | `textAlign: 'center'` should be `'start'`; some margin values unverified |
| Date formatting | **BUG** — Uses `toLocaleDateString()` with browser locale, not i18n language | MEDIUM |
| Number formatting | **BUG** — `toLocaleString()` uses browser locale, not Arabic locale | MEDIUM |
| Font loading | Cairo font configured for Arabic — verify CDN load in `index.html` | LOW |
| Hardcoded strings | None found — all UI strings use `t()` function | OK |

---

## Phase 8 — Input Validation Audit

| Area | Status | Issues |
|------|--------|--------|
| Registration validator | Excellent — regex, length, complexity rules | OK |
| Login validator | Good — NotEmpty + max length | OK |
| File upload | Excellent — type whitelist, size limit, count limit | OK |
| Admin review notes | **Missing max length** — unbounded string written to DB | MEDIUM |
| Client-side validation | Basic HTML5 only — fully relies on server validation | MEDIUM |
| XSS prevention | DOMPurify with whitelist applied on ScholarshipDetail | OK |
| SQL injection | No raw SQL found — all EF Core LINQ queries | OK |
| Email templates | **User names not HTML-escaped** in email body — stored XSS possible | MEDIUM |

---

## Phase 9 — Security Audit

### Critical Vulnerabilities

| # | Vulnerability | Location | Impact |
|---|--------------|----------|--------|
| S1 | **Google OAuth ClientSecret exposed in public repo** | `appsettings.json:25-26` | Account takeover via OAuth impersonation |
| S2 | **Empty JWT SecretKey in production config** | `appsettings.json:6` | Token validation failure at runtime |
| S3 | **JWT tokens stored in localStorage** | `authStore.ts:32-64` | Any XSS attack can steal all tokens |
| S4 | **External auth provider linking without ownership verification** | `ExternalAuthController.cs:126-152` | User A can link User B's Google account |
| S5 | **External auth token passed as URL query parameter** | `ExternalAuthController.cs:178-235` | Token leaks via server logs and referer headers |

### High Vulnerabilities

| # | Vulnerability | Location |
|---|--------------|----------|
| S6 | Refresh token validation only checks Base64 format, not DB state | `TokenService.cs:55-71` |
| S7 | Account enumeration on registration (returns 409 Conflict for existing email) | `AuthController.cs:65-69` |
| S8 | Token refresh race condition across multiple browser tabs | `api.ts:15-100` |
| S9 | Missing HSTS (Strict-Transport-Security) header | `SecurityHeadersMiddleware.cs` |
| S10 | Missing Content-Security-Policy header | `SecurityHeadersMiddleware.cs` |

### Medium Vulnerabilities

| # | Vulnerability |
|---|--------------|
| S11 | No per-endpoint rate limiting (login should be stricter than 200 req/min global) |
| S12 | Redis password stored in config file |
| S13 | No CSRF tokens (partially mitigated by JWT auth, but not defense-in-depth) |
| S14 | `adminUser` could be null in upgrade approval handler — no null check |

---

## Phase 10 — Hidden Bug Detection

| # | Bug | Type | Severity |
|---|-----|------|----------|
| H1 | `SaveScholarship` check-then-act — duplicate rows under concurrent requests | Race Condition | HIGH |
| H2 | `TrackApplication` check-then-act — duplicate rows under concurrent requests | Race Condition | HIGH |
| H3 | ViewCount increment lost when two requests read simultaneously | Race Condition | MEDIUM |
| H4 | Refresh token concurrent revocation window — brief double-use possible | Race Condition | MEDIUM |
| H5 | `adminUser` could be null in upgrade approval — properties accessed without null check | Null Reference | MEDIUM |
| H6 | Fire-and-forget `Task.Run` in AdminController swallows all exceptions silently | Silent Failure | MEDIUM |
| H7 | `isRefreshing` module-level flag not synchronized across browser tabs | Concurrency | HIGH |

---

## Phase 11 — Performance Audit

| # | Issue | Impact | Severity |
|---|-------|--------|----------|
| P1 | **`GetRecommendedScholarships` loads ALL published scholarships into memory**, then filters in application code | Will not scale past ~1K rows | CRITICAL |
| P2 | **Dashboard deadlines query unbounded** — no `.Take()`, loads all matching rows with `Include(Scholarship)` | Slow with many applications | HIGH |
| P3 | **Caching underutilized** — only 2 of 8 query handlers use the `CachingService` | Unnecessary repeated DB load | HIGH |
| P4 | **`LIKE '%search%'` cannot use indexes** — performs full table scan on every scholarship search | Slow at scale | MEDIUM |
| P5 | **JSON deserialization inside loops** — `RemindersJson` parsed per row in `DeadlineReminderJob` | Wasted CPU | MEDIUM |
| P6 | Missing `AsNoTracking()` on several read-only query handlers | Unnecessary change tracking overhead | LOW |

---

## Phase 12 — Code Quality Review

| # | Issue | Severity |
|---|-------|----------|
| Q1 | **6 dead entities** (Group, GroupMember, Post, Comment, Like, Message) — full EF config, migrations, DbSets, zero usage | HIGH |
| Q2 | **Duplicated handler pattern** — identical fetch+null-check+update logic across 4 Update handlers | MEDIUM |
| Q3 | **`Result<T>` inconsistency** — both `Error` (string) and `Errors` (List) exist; callers use them differently | MEDIUM |
| Q4 | **`AuthController` business logic** — 57 lines of onboarding orchestration belong in a command handler | MEDIUM |
| Q5 | **Unused enums** — `GroupRole`, `RejectionReasonCode` never referenced | LOW |
| Q6 | **Interface segregation violation** — `IApplicationDbContext` is incomplete, bypassed by controllers injecting concrete class | MEDIUM |

---

## Phase 13 — Database Design Review

| # | Issue | Severity |
|---|-------|----------|
| D1 | **Missing FK indexes**: `Comment.AuthorId`, `Comment.ParentCommentId`, `Like.UserId`, `Group.CreatorId` | CRITICAL |
| D2 | **JSON columns instead of proper relations**: Tags, EligibleCountries, EligibleMajors, ChecklistJson, RemindersJson | HIGH |
| D3 | **No unique constraint on `SavedScholarship(UserId, ScholarshipId)`** — allows duplicate bookmarks | HIGH |
| D4 | **No unique constraint on `ResourceBookmark` and `ResourceProgress`** | MEDIUM |
| D5 | **Notification index suboptimal** — `HasIndex(IsRead)` should be compound `(UserId, IsRead)` | MEDIUM |
| D6 | **Soft-deletable entities missing `IsDeleted` index**: Group, Post, Resource | MEDIUM |
| D7 | All FKs set to `DeleteBehavior.NoAction` — user deletion requires explicit cleanup of related records | LOW |

---

## Phase 14 — Logging & Observability

| # | Issue | Severity |
|---|-------|----------|
| L1 | **No authentication attempt logging** — cannot detect or alert on brute force attacks | HIGH |
| L2 | **No structured logging in query/command handlers** — no execution times, filter params, or cache stats | HIGH |
| L3 | **External auth silent failures** — `catch { return (null, null, null); }` with no log entry | MEDIUM |
| L4 | **No correlation IDs** — cannot trace a single request across log entries | MEDIUM |
| L5 | **Exception handler missing request context** — no path, user ID, or HTTP method in error logs | MEDIUM |
| L6 | **No centralized log aggregation** — file-based only, no ELK/Splunk/Application Insights | MEDIUM |

---

## Phase 15 — Resilience & Failure Handling

| # | Issue | Severity |
|---|-------|----------|
| R1 | **No retry logic** for failed API requests | MEDIUM |
| R2 | **No timeout-specific error messages** — generic error shown for network timeout | MEDIUM |
| R3 | **Session expiry loses page context** — redirects to `/` without saving the current URL | MEDIUM |
| R4 | **Mutation errors in `CardDetailDrawer` not surfaced** — no `onError` callbacks on mutations | MEDIUM |
| R5 | No offline support — no service worker, no queued mutation strategy | LOW |
| R6 | Upgrade status fetch silently fails in `AuthenticatedLayout` | LOW |

---

## Phase 16 — Deployment & Production Readiness

| Component | Status | Blocker? |
|-----------|--------|----------|
| **Dockerfile (API)** | MISSING | **YES** |
| **Dockerfile (Client)** | MISSING | **YES** |
| **docker-compose.prod.yml** | MISSING | **YES** |
| **Secrets management** | Hardcoded in repo | **YES** |
| HTTPS/TLS | Partial (`UseHttpsRedirection` present, no HSTS) | No |
| Health checks | DB only — no Redis or SMTP checks | No |
| Graceful shutdown | Not implemented | No |
| HSTS / CSP headers | Missing | No |
| Log aggregation | File-based only | No |
| Backup strategy | None defined | No |
| CI/CD deploy workflow | Missing (only lint/test workflows exist) | No |
| APM / Monitoring | None | No |
| Hangfire jobs | Disabled by default | No |
| Redis caching | Disabled by default | No |
| CORS `AllowedOrigins` | Hardcoded to `localhost` in appsettings | No |

---

## Phase 17 — Dependency & Library Audit

**Backend:** All .NET 10 packages are current. One concern: **Hangfire 1.8.23** is 18+ months old with no .NET 10-specific optimizations.

**Frontend:** All 24 production dependencies are current. Minor lag: `@react-oauth/google` 0.13.4 is ~3 months old.

**No known CVEs** in any current dependency (as of 2026-03-11).
**No duplicate or redundant packages** found.
**No license conflicts** — all MIT/Apache-compatible.

---

## Phase 18 — Technical Debt Analysis

| Category | Debt Item | Estimated Effort |
|----------|----------|-----------------|
| Dead Code | 6 community entities + EF configs + migrations | 4h |
| Architecture | Controllers bypass `IApplicationDbContext` | 8h |
| Architecture | Business logic in `AuthController` | 4h |
| Data Model | JSON columns for structured data (Tags, Checklist, Reminders) | 16h |
| Security | JWT tokens in localStorage | 8h |
| Testing | ~5% test coverage → ~60% meaningful coverage | 60h |
| Deployment | No containerization (Dockerfiles, compose, CI deploy) | 16h |
| Observability | No APM, no correlation IDs | 8h |
| **Total** | | **~124 hours** |

---

## Phase 19 — Refactoring Plan

### Priority 1 — Security (Week 1)
1. Rotate and remove all hardcoded credentials from the repository
2. Move JWT tokens from localStorage to httpOnly cookies
3. Add HSTS and Content-Security-Policy headers
4. Return 400 (not 409) on duplicate registration to prevent account enumeration
5. Fix external auth provider linking — require OAuth flow to verify ownership

### Priority 2 — Data Integrity (Week 1–2)
1. Add unique constraints: `SavedScholarship(UserId, ScholarshipId)`, `ApplicationTracker(UserId, ScholarshipId)`
2. Add status transition validation to `UpdateApplicationStatusCommand`
3. Fix race conditions — add DB constraints and handle `DbUpdateException`
4. Add missing FK indexes (Comment, Like, Group)

### Priority 3 — Architecture (Week 2–3)
1. Extract onboarding logic from `AuthController` into `CompleteOnboardingCommand`
2. Complete `IApplicationDbContext` so controllers can inject the interface
3. Remove 6 dead community entities, DbSets, and EF configurations
4. Replace JSON columns with proper normalized entity relations

### Priority 4 — Performance (Week 3)
1. Fix `GetRecommendedScholarships` to push all filters to the database
2. Add `.Take(10)` to dashboard deadline query
3. Expand `CachingService` usage to all read-heavy query handlers
4. Replace `LIKE '%search%'` with SQL Server full-text search

### Priority 5 — Deployment (Week 3–4)
1. Create multi-stage Dockerfiles (API + Client/nginx)
2. Create `docker-compose.prod.yml`
3. Create `appsettings.Production.json` enabling Redis and Hangfire
4. Add GitHub Actions production deploy workflow
5. Add Redis and SMTP health checks

---

## Phase 20 — Scalability & Future Architecture

**Current Bottlenecks:**

| Bottleneck | Current Behavior | Limit |
|-----------|-----------------|-------|
| Scholarship recommendations | O(N) in-memory processing | ~1K rows |
| Scholarship search | Full table scan via LIKE | ~10K rows |
| Single SQL Server | No read replicas | Single point of failure |
| Static assets | Served directly | No CDN |
| Email sending | Fire-and-forget Task.Run | Lost on failure |

**Recommendations for Scale:**
- Add Elasticsearch (or SQL Server full-text search) for scholarship search and recommendations
- Implement read replicas for query-heavy operations
- Replace fire-and-forget `Task.Run` with a proper message queue (RabbitMQ / Azure Service Bus)
- Serve the client SPA via CDN (Cloudflare, Azure CDN)
- Use SignalR for real-time notifications (WebSocket proxy already configured in Vite)

---

## Phase 21 — Product Gap Analysis

| Gap | Category | Priority |
|-----|----------|----------|
| No email verification on registration | Security | HIGH |
| Password reset flow not implemented end-to-end | Feature | HIGH |
| No admin scholarship CRUD (admin can only review upgrade requests) | Feature | MEDIUM |
| No system health/metrics dashboard for admins | Operations | MEDIUM |
| No analytics or usage tracking | Product | MEDIUM |
| No user profile completion tracking or prompts | UX | MEDIUM |
| No push notifications (web/mobile) | Engagement | LOW |
| No export functionality (PDF, CSV for application list) | Feature | LOW |
| No search history or suggestions | UX | LOW |
| No user feedback or rating system for scholarships | Product | LOW |

---

## Phase 22 — Test Coverage Review

| Area | Coverage | Status |
|------|----------|--------|
| Auth flow (register, login, refresh, onboarding) | 7 integration tests | Moderate |
| Validator rules (onboarding, upgrade requests) | 6 unit tests | Moderate |
| Controllers (9 total) | 0 tests | **CRITICAL GAP** |
| Command handlers (8 total) | 0 tests | **CRITICAL GAP** |
| Query handlers (6 total) | 0 tests | **CRITICAL GAP** |
| Services (Token, Email, Cache) | 0 tests | HIGH GAP |
| Frontend pages (15+) | 0 tests | HIGH GAP |
| Frontend components (20+) | 1 test (ProtectedRoute only) | HIGH GAP |
| Frontend hooks and stores | 0 tests | HIGH GAP |
| E2E tests | 0 | **CRITICAL GAP** |
| Performance / load tests | 0 | MEDIUM GAP |

**Estimated total coverage: ~5–8%**

---

## Phase 23 — Final Professional Audit Report

### 1. Critical Issues (Must Fix Before Launch)

| # | Issue | Phase |
|---|-------|-------|
| 1 | Google OAuth ClientSecret exposed in public repository — **REVOKE IMMEDIATELY** | 9 |
| 2 | Empty JWT SecretKey in production config | 9 |
| 3 | No Dockerfiles — cannot deploy to any cloud platform | 16 |
| 4 | External auth provider linking without ownership verification — account takeover risk | 9 |
| 5 | `GetRecommendedScholarships` loads all rows into memory — will not scale | 11 |
| 6 | No application status transition validation | 4 |
| 7 | Race conditions on save/track (check-then-act without DB constraints) | 10 |
| 8 | Missing critical FK indexes (Comment, Like, Group) | 13 |

### 2. Major Improvements

| # | Improvement | Phase |
|---|-------------|-------|
| 1 | Move tokens from localStorage to httpOnly cookies | 9 |
| 2 | Add HSTS and Content-Security-Policy headers | 16 |
| 3 | Fix account enumeration on registration | 9 |
| 4 | Add unique constraints on SavedScholarship and ApplicationTracker | 13 |
| 5 | Extract business logic from `AuthController` to command handlers | 12 |
| 6 | Remove 6 dead community entities | 12 |
| 7 | Replace JSON columns with proper normalized entity relations | 13 |
| 8 | Add structured logging and correlation IDs throughout | 14 |

### 3. Minor Improvements

| # | Improvement | Phase |
|---|-------------|-------|
| 1 | Fix date and number formatting for Arabic locale | 7 |
| 2 | Add loading indicators for async mutations | 6 |
| 3 | Improve Kanban empty state messages | 6 |
| 4 | Add `.Take()` to dashboard deadlines query | 11 |
| 5 | Expand caching to more query handlers | 11 |
| 6 | Fix notes field character limit UX | 6 |

### 4. Security Vulnerabilities

| Severity | Count | Key Items |
|----------|-------|-----------|
| CRITICAL | 5 | Exposed OAuth secret, empty JWT key, localStorage tokens, auth linking bypass, token in URL |
| HIGH | 5 | Incomplete refresh token validation, account enumeration, multi-tab race condition, missing HSTS/CSP |
| MEDIUM | 6 | No per-endpoint rate limiting, Redis password in config, email HTML injection, admin null check, no CSP |

### 5. Performance Improvements

| Priority | Item |
|----------|------|
| CRITICAL | Fix `GetRecommendedScholarships` to push filters to the database |
| HIGH | Add pagination/`.Take()` to dashboard deadlines query |
| HIGH | Expand caching strategy to all read-heavy handlers |
| MEDIUM | Replace LIKE search with SQL Server full-text search |
| MEDIUM | Add missing database indexes |
| LOW | Document and implement a connection pooling strategy |

### 6. UX Improvements

| Priority | Item |
|----------|------|
| HIGH | Add delete confirmation dialog to TrackerCard (list view) |
| HIGH | Make hover-only actions accessible via keyboard and touch |
| MEDIUM | Add proper error states to Dashboard, Tracker, and Admin pages |
| MEDIUM | Fix date and number formatting per i18n locale |
| MEDIUM | Add empty state messages with calls to action |
| LOW | Adjust responsive drawer width for mobile landscape |

### 7. Missing Features

| Priority | Feature |
|----------|---------|
| HIGH | Email verification on registration |
| HIGH | Password reset end-to-end flow |
| MEDIUM | Admin scholarship CRUD |
| MEDIUM | System health dashboard |
| MEDIUM | Analytics and usage tracking |
| LOW | Push notifications, data export, search suggestions |

### 8. Technical Debt

| Category | Effort |
|----------|--------|
| Dead code removal (6 community entities) | 4h |
| Architecture fixes (DI violations, SRP) | 12h |
| Data model normalization (JSON → entities) | 16h |
| Security hardening (token storage, headers) | 8h |
| Test coverage (~5% → ~60%) | 60h |
| Deployment infrastructure (Docker, CI/CD) | 16h |
| Observability (APM, correlation IDs) | 8h |
| **Total** | **~124h** |

### 9. Test Coverage Gaps

- **0% coverage** on: Controllers, Command handlers, Query handlers, Services, all Frontend pages, components, and hooks
- **Moderate coverage** on: Auth flow integration tests, FluentValidation unit tests
- **Zero** E2E, performance, or visual regression tests
- **No coverage enforcement** in CI pipeline (Coverlet installed but not configured as a gate)

### 10. Production Readiness Assessment

| Dimension | Score | Status |
|-----------|-------|--------|
| Functionality | 6/10 | Core features work; community not implemented |
| Security | 3/10 | Critical vulnerabilities present |
| Performance | 5/10 | Works at small scale; critical scaling issues exist |
| Code Quality | 6/10 | Clean Architecture followed; dead code and violations |
| Testing | 2/10 | ~5% coverage; no E2E |
| Deployment | 2/10 | No Docker, no secrets management, no production config |
| Observability | 3/10 | Basic file logging only; no APM or tracing |
| Resilience | 4/10 | Some error handling; no retry or offline support |
| UX / Accessibility | 6/10 | Functional but accessibility and mobile gaps |
| i18n | 7/10 | Complete translations; minor date/number formatting bugs |

---

## Is the Application Production-Ready?

**No.** The application is **not production-ready** due to:

1. Critical security vulnerabilities (exposed secrets, account takeover risk, XSS amplification via localStorage)
2. No containerization — cannot deploy to any cloud platform
3. ~5% test coverage — reliability of core features is unknown
4. Data integrity risks — race conditions, missing unique constraints, no status transition validation
5. Critical performance blocker — recommendation engine loads all rows into memory

---

## Prioritized Action Plan

### Week 1 — Security & Blockers
1. **Revoke and rotate** Google OAuth credentials immediately
2. **Create Dockerfiles** for API (multi-stage .NET 10) and Client (nginx)
3. Move all secrets to environment variables or a secrets vault
4. Add HSTS and Content-Security-Policy headers to `SecurityHeadersMiddleware`
5. Fix external auth linking vulnerability
6. Add unique constraints to prevent duplicate saves and tracked applications
7. Add application status transition validation

### Week 2 — Data Integrity & Architecture
1. Fix race conditions using DB constraints + catch `DbUpdateException`
2. Add missing FK indexes (Comment, Like, Group)
3. Extract business logic from controllers into command handlers
4. Remove dead community entities, DbSets, and configurations
5. Fix account enumeration (return 400 instead of 409)
6. Add authentication attempt logging

### Week 3 — Testing & Performance
1. Write unit tests for all command and query handlers
2. Write integration tests for all controllers
3. Fix `GetRecommendedScholarships` to filter at the database level
4. Expand caching to all read-heavy query handlers
5. Add `.Take()` to unbounded dashboard queries

### Week 4 — Deployment & Polish
1. Create `docker-compose.prod.yml`
2. Create `appsettings.Production.json` enabling Redis and Hangfire
3. Add GitHub Actions production deploy workflow
4. Configure health checks for Redis and SMTP
5. Add APM/monitoring integration
6. Fix UX gaps (delete confirmation, mobile accessibility, error states)
7. Fix i18n date and number formatting
