# ScholarPath — Testing Evidence Report

**Purpose:** Factual testing evidence to support the later authoring of *Chapter 5: Testing and Evaluation*.
**Nature:** Evidence report only. This document does **not** constitute Chapter 5 and does **not** invent pass/fail results.
**Prepared by:** QA / testing documentation review of the repository.
**Date of analysis:** 2026-07-03
**Branch analysed:** `claude/scholarpath-testing-report-57we9m`
**Repository:** `ma7moudalysalem/scholarpath`

---

## 0. How to read this report (evidence provenance)

Every result below is labelled with one of two provenance tags:

| Tag | Meaning |
|-----|---------|
| **Executed** | The test command was actually run in this analysis environment and the output is reproduced/summarised here. |
| **Code inspection only** | The tests exist in the repository and were read, but could **not** be executed in this environment (see the constraints in §1 and §3). No pass/fail is asserted for these. |

**Environment constraints that shaped what could be executed:**

- **.NET SDK is not installed** in the analysis environment, and the required SDK (`10.0.201`, per `server/global.json`) could **not** be installed because the outbound egress policy blocks the Microsoft download host (`builds.dotnet.microsoft.com` → HTTP 403). Therefore **no backend `dotnet test` command could be executed.**
- **Docker is not running** in the analysis environment. The integration test project relies on `Testcontainers.MsSql` (a real SQL Server container per test class), so **integration tests could not be executed** even had the SDK been present.
- **Frontend Vitest unit tests were executed successfully** (Node 22 + npm are available; `registry.npmjs.org` is reachable).
- **Playwright E2E tests could not be executed:** the pre-installed browser build does not match the version this project's Playwright pins, and the matching browser download host (`cdn.playwright.dev`) is egress-blocked (HTTP 403). The E2E suite is also designed to run against a **deployed** environment with seeded login credentials (see §3), which are not present here.

---

## 1. Testing Environment

| Aspect | Detail |
|--------|--------|
| Backend framework | ASP.NET Core / .NET 10 (`net10.0`), Clean-Architecture solution `server/ScholarPath.slnx` (API, Application, Domain, Infrastructure) |
| Backend patterns under test | MediatR command/query handlers, FluentValidation validators, EF Core, Hangfire background jobs, SignalR hubs |
| Required backend SDK | .NET SDK `10.0.201` (`server/global.json`, `rollForward: latestFeature`) |
| Frontend framework | React 19 + TypeScript + Vite 8 (`client/`), TanStack Query, React Router 7, i18next (EN/AR, RTL) |
| Backend unit test framework | **xUnit** + **FluentAssertions** + **NSubstitute** + **Bogus** (fake data) + **MockQueryable.NSubstitute**; EF Core **InMemory** and **Sqlite** providers; `coverlet` for coverage |
| Backend integration test framework | **xUnit** + **FluentAssertions** + **Microsoft.AspNetCore.Mvc.Testing** (`WebApplicationFactory<Program>`) + **Testcontainers.MsSql** + **Respawn** (DB reset) |
| Frontend unit test framework | **Vitest 4** + **@testing-library/react** + **@testing-library/user-event** + **@testing-library/jest-dom**; DOM via **happy-dom** |
| Frontend E2E framework | **Playwright** (`@playwright/test`), projects: Chromium, Firefox, mobile (Pixel 7) |
| Test database (unit) | EF Core InMemory / SQLite in-memory (no external DB) |
| Test database (integration) | SQL Server 2022 via Testcontainers (`mcr.microsoft.com/mssql/server:2022-latest`) — **requires Docker** |
| Commands executed in this analysis | `npm install` (client) ✅; `npm test` → `vitest run` (client) ✅; `dotnet test` ❌ (SDK unavailable); `npx playwright test smoke.spec.ts` ❌ (browser unavailable) |

---

## 2. Existing Test Assets

Backend test projects live under `server/tests/`; frontend tests under `client/src/test/`.

| Area | Test File / Project | Test Type | Purpose |
|------|--------------------|-----------|---------|
| Backend — all unit tests | `server/tests/ScholarPath.UnitTests/` (~111 test classes) | Unit (xUnit) | Handler logic, validators, domain rules, background jobs, security utilities |
| Backend — all integration tests | `server/tests/ScholarPath.IntegrationTests/` | Integration (xUnit + Testcontainers) | Full-API endpoint round-trips against a real SQL Server |
| Authentication / Onboarding | `Auth/LoginCommandHandlerTests`, `RegisterCommandHandlerTests`, `SelectRoleCommandHandlerTests`, `EmailVerificationTests`, `JwtRs256Tests`, `MemorySsoStateStoreTests`, `EmailChangeValidatorTests`, `RequestEmailChangeCommandHandlerTests` | Unit | Login/lockout, registration, role selection + onboarding gating, email verification, RS256 JWT, SSO state nonce |
| Profile & Account | `Profile/UpdateProfileCommandHandlerTests`, `UpdateProfileCommandValidatorTests`, `ChangePasswordCommandValidatorTests`, `ProfilePhotoUploadValidationTests` | Unit | Profile update logic + validation, password-change rules, photo upload validation |
| Scholarship Discovery | `Scholarships/ScholarshipBookmarksAndFeaturedTests`, `ScholarshipTests`, `Scholarships/ScholarshipAutoCloseJobTests` | Unit | Bookmarks list, featured list, localisation (AR), auto-close job; domain defaults |
| Local Scholarship Mgmt | `Scholarships/ScholarshipTests`, `AllowFreeScholarshipsSettingTests` | Unit | Scholarship domain state defaults, free-scholarship setting |
| External Scholarship | `Applications/ExternalIntentCommandHandlerTests` | Unit | External-listing "intending" application, tracking-URL validation, re-application |
| Application Submission & Tracking | `Applications/CreateApplicationCommandHandlerTests`, integration `Applications/CreateApplicationCommandHandler` | Unit + Integration | Draft create/resume, status-transition state machine, active/terminal/read-only rules |
| Application Responding | `ScholarshipProviderReviewRequests/*` (Start, Accept, Cancel, ConfirmHoldAndComplete, RejectAndExpire, notifications), `ScholarshipProviderReviews/ApplicationStatusChangedEventHandlerTests` | Unit | Provider responding workflow, status-change events |
| Consultant Discovery & Booking | `ConsultantBookings/*` (BookingQueryHandler, ConsultantQueryHandler, ConsultantReadService, Reschedule, MeetingNoShow, BookingReminderJob, SessionRecording, RequestBookingMasterSwitch) + integration endpoint tests (Request/Accept/Reject/Cancel/MarkNoShow) | Unit + Integration | Booking lifecycle, availability, consultant discovery/read, no-show, reminders |
| Community | `Community/*` (CreatePost, UpdatePost, DeletePost, CreateReply, FlagPost, ToggleVote, ToggleBookmark, ForumReplyCreatedEventHandler) + `TagInput.test.tsx` (frontend) | Unit | Post/reply CRUD, role permissions, tags, votes, bookmarks, flagging |
| Messaging | `Chat/SendMessageCommandHandlerTests`, `Chat/GetConversationsQueryHandlerTests`, `Chat/ChatContactReadServiceTests`, integration `Chat/BlockEnforcementIntegrationTests`, `Hubs/PresenceTrackerTests` | Unit + Integration | Send/list conversations, contact search filters, block enforcement, presence |
| Resources Hub | `Resources/*` (CreateResource, SearchResources, ResourcePublishWorkflow, FeatureResource, ResourceStudentActions) | Unit | Resource publish workflow (draft→review→publish), search/filter, student actions |
| AI Advisory | `Ai/*` (AskChatbotRedaction, EligibilityVerdict, AiCostGate, LogRecommendationClick, KnowledgeBaseIndexer), integration `Ai/RedactionAuditSamplingJobTests` | Unit + Integration | PII redaction, eligibility verdict, cost gating, recommendation click logging, redaction audit sampling |
| Dashboards / Role-Based Views | Frontend `ProfileMenu.test.tsx` (role switcher); E2E route-guard specs `auth.spec.ts` | Unit (FE) + E2E | Role switching UI; route guards per role |
| Notifications | `Notifications/*` (NotificationCatalog, NotificationDispatcher, NotificationPreferences, ReminderJobs), integration `Notifications/EmailDeliveryIntegrationTests` | Unit + Integration | Catalog, dispatch, preference matrix, reminder jobs, email delivery + idempotency |
| Admin Oversight | `Admin/*` (ChangeUserRole, SetUserStatus, UserAdministration, ExportUsersCsv, ReviewOnboardingRoleGrant, ReviewUpgradeRequestValidator, SendBroadcast, LowRatedCompanies), integration `Admin/AtRiskUserChipTests`, `UpgradeRequests/SubmitConsultantUpgradeRequestCommandHandlerTests` | Unit + Integration | Role/status admin, onboarding review, broadcasts, at-risk/low-rated flags, upgrade requests |
| Security, Privacy & Audit | `Security/*` (FieldEncryption, FieldEncryptionConverter, FileScanning), `Audit/*` (AuditBehavior, DataExportJob, DataDeleteJob), `Common/ReviewerNameMaskTests`, `Documents/DocumentVaultTests` | Unit | Field encryption round-trip/tamper, file scanning, audit logging, GDPR export/delete, reviewer-name masking, document vault access |
| Frontend — unit/component | `client/src/test/*.test.tsx` (Home, ProfileMenu, ResetPassword, TagInput, usePlatformStatus) | Unit (Vitest) | Page render, role-switcher, password rules, tag input, platform-status hook |
| Frontend — E2E | `client/src/test/e2e/*.spec.ts` (14 specs) | E2E (Playwright) | Cross-module user journeys (see §3 and §6) |
| **Removed-scope (financial/payment)** | `Payments/*`, `ProfitShare/*`, `FinancialConfig/*`, `Scholarships/{CreateScholarshipReviewFeeValidator, PaymentsEnabledMasterSwitch}`, `ConsultantBookings/{RefundCalculatorService, BookingPaymentSync}`, `ScholarshipProviderReviews/{Capture/Refund/RejectPayment/TimeoutRefundJob}`, integration `Payments/PaymentsIntegrationTests`, e2e `payments.spec.ts` | Unit + Integration + E2E | **Out of final scope** — see §Scope Alignment note below |

### Scope Alignment note

The repository still contains a substantial body of tests exercising **removed financial/payment scope** (Stripe payment intents, Stripe Connect, refunds, payouts, review fees, profit-share configuration/calculation, and the "payments-enabled" master switch). Per the final ScholarPath scope, these are **excluded** from the in-scope module evaluation in this report. They remain in the repository and are listed above and in §6 for completeness, but no in-scope pass/fail conclusion should be drawn from them.

**Terminology used in this report** (final scope): *Scholarship Provider* (the code and some routes still use "Company"), *Application Responding* (code namespace: `ScholarshipProviderReviewRequests`), *In Assessment* (vs. legacy "UnderReview"), *Local Scholarships*, *External Scholarships*, *Resources Hub*, *Admin-approved Consultants*. Where a test/file name uses legacy terminology, the original name is preserved for traceability.

---

## 3. Executed Test Commands

| Command | Location | Result | Notes |
|---------|----------|--------|-------|
| `npm install` | `client/` | **Executed — success (exit 0)** | Dependencies installed from `registry.npmjs.org`. |
| `npm test` (`vitest run`) | `client/` | **Executed — PASS: 5 files, 17/17 tests passed** | Duration ~3.0 s. One non-fatal `ECONNREFUSED 127.0.0.1:3000` was logged (a stray/unmocked network attempt) but did **not** fail any test; the run exited 0. |
| `npx vitest run --reporter=verbose` | `client/` | **Executed — 17/17 passed** | Used to capture individual test names (see §4). |
| `dotnet test … ScholarPath.UnitTests` | `server/` | **NOT executed** | `dotnet` not installed; SDK `10.0.201` download blocked by egress policy (`builds.dotnet.microsoft.com` → 403). |
| `dotnet test … ScholarPath.IntegrationTests` | `server/` | **NOT executed** | Requires .NET SDK **and** Docker (Testcontainers SQL Server). Neither available here. |
| `npx playwright test smoke.spec.ts --project=chromium` | `client/` | **NOT executed (browser launch failed)** | Pre-installed browser build (v1194) ≠ version pinned by this Playwright (v1217); matching browser download blocked (`cdn.playwright.dev` → 403). |
| `npx playwright install chromium-headless-shell` | `client/` | **Failed (egress 403)** | `cdn.playwright.dev` host not permitted by egress policy. |

**Reference commands defined by the project** (`docs/TESTING.md`, CI workflows) — provided for Chapter 5 reproducibility, not executed here:

- Backend, all: `cd server && dotnet test`
- Backend, unit only (CI): `dotnet test server/tests/ScholarPath.UnitTests/ScholarPath.UnitTests.csproj --configuration Release --collect:"XPlat Code Coverage"`
- Backend, integration (CI, push to `main`/`integration` only, `continue-on-error`): `dotnet test server/tests/ScholarPath.IntegrationTests/ScholarPath.IntegrationTests.csproj --configuration Release`
- Frontend, unit: `cd client && npm run test`
- Frontend, E2E (needs live target + seeded creds): `cd client && npm run test:e2e`

---

## 4. Unit Testing Evidence

### 4a. Frontend unit tests — **Executed (Vitest), 17/17 passed**

| Module | Tested Functionality | Test File(s) | Result | Notes |
|--------|---------------------|--------------|--------|-------|
| Public site / Discovery entry | Home page renders an `<h1>` hero heading | `Home.test.tsx` (1) | **PASS (Executed)** | Renders under Router + React Query. |
| Dashboards / Role-based views | Role switcher: hidden for single-role, shown for dual-role, calls switch-role API | `ProfileMenu.test.tsx` (3) | **PASS (Executed)** | Mocks notifications + auth APIs. |
| Authentication (reset password) | Invalid-link state; rejects short / missing-uppercase-digit-special / mismatched; accepts valid | `ResetPassword.test.tsx` (5) | **PASS (Executed)** | Password rules mirror Register. |
| Community | Tag commit + slugify on Enter; case-insensitive dedupe; caps at 5 tags; chip removal | `TagInput.test.tsx` (4) | **PASS (Executed)** | Cap constant `MAX_TAGS_PER_POST = 5`. |
| Platform status (payments toggle surface) | Safe defaults before resolve; reflects `paymentsEnabled` true/false; fallback true on error | `usePlatformStatus.test.tsx` (4) | **PASS (Executed)** | Confirms the client honours a server "payments disabled" flag. |

### 4b. Backend unit tests — **Code inspection only (not executed)**

Counted by `[Fact]`/`[Theory]` attributes: **≈696 unit test methods across ~111 test classes.** Actual executed case count would be **higher**, because `[Theory]` methods expand into multiple cases via `[InlineData]`. **No pass/fail is asserted** — the suite was not run.

| Module | Tested Functionality (representative) | Test File(s) | Result | Notes |
|--------|--------------------------------------|--------------|--------|-------|
| Authentication / Onboarding | Valid login→tokens; wrong password/unknown email→conflict; lockout on 5th failure; role selection activates student / queues provider onboarding / persists consultant profile; onboarding blocked without documents; RS256 JWT; SSO state nonce | `Auth/LoginCommandHandlerTests` (5), `SelectRoleCommandHandlerTests` (20), `RegisterCommandHandlerTests` (3), `EmailVerificationTests` (7), `JwtRs256Tests` (4), `MemorySsoStateStoreTests` (3), `EmailChangeValidatorTests` (5), `RequestEmailChangeCommandHandlerTests` (3) | Code inspection only | Strong branch coverage on auth flows. |
| Profile & Account | Profile update persistence; extensive field validation (25 validator cases); password-change rules; photo upload validation | `Profile/UpdateProfileCommandHandlerTests` (7), `UpdateProfileCommandValidatorTests` (25), `ChangePasswordCommandValidatorTests` (9), `ProfilePhotoUploadValidationTests` (6) | Code inspection only | — |
| Scholarship Discovery | Bookmarks (own-only, ordering, orphan drop, AR localisation, forbidden-when-anon); featured (open+non-deleted only, ordering, limit, AR); auto-close job | `Scholarships/ScholarshipBookmarksAndFeaturedTests` (11), `ScholarshipAutoCloseJobTests` (5), `ScholarshipTests` (4) | Code inspection only | **No dedicated scholarship search/filter/pagination query-handler test found** (see §6). |
| Local Scholarship Mgmt | Scholarship domain defaults (Draft), free-scholarship setting behaviour | `ScholarshipTests` (4), `AllowFreeScholarshipsSettingTests` (4) | Code inspection only | Local scholarship create/edit lifecycle mostly covered via integration + provider-review suites. |
| External Scholarship | External-listing intending application; not-found; rejects in-app listing; duplicate-active conflict; re-apply after terminal; tracking-URL validation | `Applications/ExternalIntentCommandHandlerTests` (12) | Code inspection only | — |
| Application Submission & Tracking | Draft create/resume/no-duplicate; fresh draft after terminal; status-transition state machine (allowed/invalid transitions, active/terminal/read-only); ownership → NotFound/Conflict | `Applications/CreateApplicationCommandHandlerTests` (19) | Code inspection only | Rich state-machine coverage. |
| Application Responding | Provider responding request: create + (fee path) capture intent; free path skips payment→Pending; idempotent double-click; rejects own-scholarship / closed / no-fee; start/accept/cancel/confirm/reject-expire; status-changed event | `ScholarshipProviderReviewRequests/*` (Start 8, Accept 5, Cancel 6, ConfirmHoldAndComplete 5, RejectAndExpire 5, notif factory 2), `ScholarshipProviderReviews/ApplicationStatusChangedEventHandlerTests` (3) | Code inspection only | Fee/Stripe branches are removed-scope; the **free/no-payment** responding path is in-scope. |
| Consultant Discovery & Booking (no payment) | My/consultant bookings (own-only, soft-delete excluded, forbidden); booking-by-id authorisation matrix; availability round-trip; reschedule; no-show; reminder jobs; consultant read/discovery projections | `ConsultantBookings/BookingQueryHandlerTests` (16), `ConsultantReadServiceTests` (19), `ConsultantQueryHandlerTests` (5), `RescheduleBookingCommandHandlerTests` (6)+validator (3), `MeetingNoShowTests` (8), `BookingReminderJobTests` (9), `SessionRecordingTests` (7), `RequestBookingMasterSwitchTests` (2) | Code inspection only | Payment-coupled files (`RefundCalculatorService`, `BookingPaymentSync`) are removed-scope. |
| Community | Student creates post; consultant/provider cannot; tag normalisation + >5 tags error; reply/delete/update; flag; vote toggle; bookmark toggle; forum-reply event | `Community/CreatePostCommandHandlerTests` (5), UpdatePost (5), DeletePost (4), CreateReply (3), FlagPost (3), ToggleVote (3), ToggleBookmark (3), ForumReplyCreatedEventHandler (3) | Code inspection only | Role-permission matrix covered. |
| Messaging | Blocked-user send→conflict; conversation listing; contact search filters (excludes self/inactive/soft-deleted/blocked, name match, projection, limit, ordering); presence tracking | `Chat/SendMessageCommandHandlerTests` (1), `GetConversationsQueryHandlerTests` (5), `ChatContactReadServiceTests` (11), `Hubs/PresenceTrackerTests` (7) | Code inspection only | **Only 1 unit test on the send-message handler**; real-time SignalR broadcast not unit-tested (see §6). |
| Resources Hub | Publish workflow (draft→pending→publish; admin direct publish; approve/reject; author-bio gate); search (published-only, category, title match); create; feature; student actions | `Resources/ResourcePublishWorkflowTests` (7), `SearchResourcesQueryHandlerTests` (3), `CreateResourceCommandHandlerTests` (4), `FeatureResourceCommandHandlerTests` (4), `ResourceStudentActionsTests` (4) | Code inspection only | — |
| AI Advisory | PII redaction patterns; empty/long-message guards; eligibility verdict; AI cost gate; recommendation-click logging; knowledge-base indexer | `Ai/AskChatbotRedactionTests` (6), `EligibilityVerdictTests` (7), `AiCostGateTests` (5), `LogRecommendationClickTests` (9), `KnowledgeBaseIndexerTests` (1) | Code inspection only | KB indexer has only 1 case; RAG retrieval quality not unit-tested (expected). |
| Notifications | Preference matrix (defaults, disable/enable/re-enable, no-leak, auth); catalog; dispatcher; reminder jobs | `Notifications/NotificationPreferencesTests` (11), `NotificationCatalogTests` (8), `ReminderJobsTests` (9), `NotificationDispatcherTests` (3) | Code inspection only | — |
| Admin Oversight | Change role; set status; user administration; CSV export; onboarding role-grant review; upgrade-request validator; broadcasts; low-rated provider flag | `Admin/ChangeUserRoleCommandTests` (5), `SetUserStatusCommandTests` (6), `UserAdministrationTests` (5), `ExportUsersCsvQueryHandlerTests` (4), `ReviewOnboardingRoleGrantTests` (4), `ReviewUpgradeRequestValidatorTests` (4), `SendBroadcastCommandHandlerTests` (4), `LowRatedCompanies/LowRatedCompaniesTests` (8), `UpgradeRequests/SubmitConsultantUpgradeRequestCommandHandlerTests` (12) | Code inspection only | — |
| Security, Privacy & Audit | Field encryption round-trip/null/nonce-randomness/tamper/short-envelope/wrong-key; key-provider guards; EF value converter; file scanning; audit behaviour pipeline; GDPR export (all tables, audit entry, idempotent); GDPR delete; reviewer-name masking; document vault access | `Security/FieldEncryptionTests` (14), `FieldEncryptionConverterTests` (3), `FileScanningTests` (5), `Audit/AuditBehaviorTests` (4), `DataExportJobTests` (5), `DataDeleteJobTests` (7), `Common/ReviewerNameMaskTests` (5), `Documents/DocumentVaultTests` (15) | Code inspection only | Strong privacy/security coverage. |
| Domain smoke | Entity defaults (user Unassigned, scholarship Draft, tracker Active/status) | `Smoke/SmokeTests` (4) | Code inspection only | — |
| Streaming | Domain-event publishing handler | `Streaming/DomainEventPublishingHandlerTests` (5) | Code inspection only | — |
| Common | Country normaliser | `Common/CountryNormalizerTests` (5) | Code inspection only | — |

---

## 5. Integration Testing Evidence

**Code inspection only (not executed).** Counted by `[Fact]`/`[Theory]`: **≈35 integration test methods.** These use `WebApplicationFactory<Program>` + `Testcontainers.MsSql` (real SQL Server) + `Respawn`, and cannot run without Docker. In CI they run **only on push to `main`/`integration`** and with `continue-on-error: true` (informational, non-blocking).

| Scenario | Components Integrated | Test File(s) | Result | Notes |
|----------|----------------------|--------------|--------|-------|
| Start application via API | HTTP endpoint → MediatR → EF Core → SQL Server | `Applications/CreateApplicationCommandHandler` (5): valid→201; duplicate-active→409; closed→409; external→409; empty id→400 | Code inspection only | In-scope. |
| Messaging block enforcement | Chat endpoint → block rules → DB | `Chat/BlockEnforcementIntegrationTests` (4): no-block→200; recipient-blocked-sender→409; sender-blocked-recipient→409; after-unblock→200 | Code inspection only | In-scope. |
| Consultant booking endpoints | API → booking domain → DB | `ConsultantBookings/RequestBookingEndpointTests` (1), `AcceptBookingEndpointTests` (1), `RejectBookingEndpointTests` (1), `CancelBookingEndpointTests` (1), `MarkNoShowEndpointTests` (1) | Code inspection only | In-scope (booking lifecycle). |
| Admin at-risk user chip | Admin search → risk-flag join → DTO | `Admin/AtRiskUserChipTests` (3): true when flag row; false when none; false when flag=false | Code inspection only | In-scope. |
| Email delivery | Notification dispatch → email channel → persistence | `Notifications/EmailDeliveryIntegrationTests` (3): correct recipient; idempotency dedupe; persists dispatch-succeeded row | Code inspection only | In-scope. |
| AI redaction audit sampling | Hangfire job → interactions → sampling | `Ai/RedactionAuditSamplingJobTests` (3): idempotent for fully-sampled month; only samples previous month | Code inspection only | In-scope. |
| Scholarship API | `WebApplicationFactory` + Testcontainers factory | `Scholarships/ScholarshipApiTests` | **No test methods** | ⚠️ File defines only the `ScholarshipApiFactory` (32 lines) — **0 executable tests.** |
| Scholarship applications (extended) | `WebApplicationFactory` + Testcontainers + seed | `Applications/ScholarshipApplicationsIntegrationTests` | **No test methods** | ⚠️ File defines only the factory + student seed helper (117 lines) — **0 executable tests.** |
| Payments (removed scope) | Stripe/payment endpoints → DB | `Payments/PaymentsIntegrationTests` (12) | Code inspection only | **Removed scope — excluded from evaluation.** |

---

## 6. Failed / Skipped / Missing Tests

No test *failures* can be reported for the backend or E2E suites because **they were not executed** (see §1/§3). The executed frontend Vitest suite reported **0 failures**. The items below are structural gaps and execution blockers found by inspection.

| Area | Issue | Impact | Recommended Action |
|------|-------|--------|--------------------|
| Backend execution | .NET SDK `10.0.201` unavailable and un-installable here (egress-blocked); `dotnet test` not run | No executed backend pass/fail evidence in this environment | Run `dotnet test` on a machine/CI with .NET 10 SDK; attach `.trx` + coverage `.cobertura.xml` as Chapter 5 evidence |
| Integration execution | Requires Docker (Testcontainers SQL Server); Docker not running here | No executed integration evidence; CI also runs them `continue-on-error` (non-blocking) | Run integration job with Docker; capture results; consider promoting critical in-scope endpoints to a blocking check |
| E2E execution | Playwright browser version mismatch (v1194 present vs v1217 required) + download host blocked; suite targets a deployed URL with seeded creds | E2E cannot run locally here; many specs self-skip without credentials | Run E2E via the `e2e.yml` workflow against a deployed environment with `E2E_*` secrets; archive the Playwright HTML/JUnit report |
| Integration: Scholarship API | `Scholarships/ScholarshipApiTests.cs` contains **only a factory, 0 tests** | Public scholarship endpoints have **no integration coverage** despite an existing harness | Add integration tests (list/detail/create/close) using the existing `ScholarshipApiFactory` |
| Integration: Scholarship applications | `Applications/ScholarshipApplicationsIntegrationTests.cs` contains **only a factory + seed, 0 tests** | Intended extended application integration tests are **not implemented** | Implement the intended endpoint tests on top of the seeded factory |
| Scholarship Discovery (search) | **No dedicated search/filter/pagination query-handler unit test** (only bookmarks/featured/auto-close) | The primary discovery experience (keyword/eligibility/country/level filters) is **weakly tested** | Add unit tests for the scholarship search/filter/sort/pagination handler |
| Messaging (real-time) | SignalR hub message **broadcast/delivery** is not integration-tested; send-message handler has **1 unit test** | Real-time chat delivery, typing/read state untested end-to-end | Add a SignalR `HubConnection` integration test for send→receive; add handler unit tests (self-send, non-contact, length limits) |
| Dashboards / Role-based views | **No backend dashboard query-handler tests**; dashboards covered only by (non-executable-here) E2E route guards | Dashboard aggregation/role-scoping logic unverified at unit level | Add unit tests for each role's dashboard aggregation query |
| E2E self-skips | ~23 `test.skip`/`test.fixme` guards across specs (e.g., admin 2, analytics 3, applications 2, booking 2, community 2, notifications 2, profile 2, scholarships 2); auth specs also self-skip via `hasCreds(role)` when `E2E_*` secrets are absent | Reported E2E pass counts can be **misleadingly low** if run without seeded credentials | In Chapter 5, always report skipped counts alongside passed; provision credentials for the target environment |
| Frontend unit breadth | Only **5** frontend unit test files (17 cases) for a large React app | Most pages/components rely on E2E only | Add component tests for key forms (Login/Register/Onboarding), application submission, booking, messaging UI |
| Coverage gate | `docs/TESTING.md` states a **≥70%** target "enforced by CI", but `ci.yml` collects coverage **without** a failing threshold gate (the `/p:Threshold=70` gate is marked roadmap) | Coverage claim is aspirational, not enforced | Either enforce the threshold in CI or state the measured coverage % (from a real run) in Chapter 5 |
| Removed-scope tests present | Payment/Stripe/refund/profit-share/fee tests still in the repo (unit + integration + `payments.spec.ts`) | Risk of accidentally citing out-of-scope results as product evidence | Exclude from Chapter 5 evaluation; optionally remove or quarantine to match final scope |

---

## 7. Recommended Chapter 5 Test Cases

These are **recommendations to document/execute later** — not executed results. They target in-scope modules, prioritising the gaps in §6. "Related Requirement" cites identifiers actually seen in the repo where available (e.g., `FR-AUTH-13`, E2E tag `PB-001 / T-018`); otherwise a descriptive use-case name is used.

| Test Case ID | Module | Actor | Scenario | Preconditions | Test Steps | Expected Result | Related Req / Use Case |
|--------------|--------|-------|----------|---------------|-----------|-----------------|------------------------|
| TC-01 | Authentication | Registered student | Successful login issues tokens | Active verified account exists | Submit valid email+password to `/api/auth/login` | 200 + access/refresh tokens; user in Active state | Login use case (cf. `LoginCommandHandlerTests`) |
| TC-02 | Authentication | Registered student | Account locks after repeated failures | Account exists | Submit wrong password 5× | 5th attempt locks account; subsequent login rejected | Lockout (cf. `Handle_FifthFailedAttempt_LocksAccount`) |
| TC-03 | Onboarding | New Scholarship Provider | Provider selection enters onboarding queue | Authenticated, role Unassigned, verification docs uploaded | Select "Scholarship Provider" role with required fields + docs | Account queued for admin approval; active admins notified with deep link | `SelectRoleCommandHandlerTests`; onboarding UC |
| TC-04 | Onboarding | New Provider (no docs) | Provider onboarding blocked without documents | Authenticated, no verification docs | Attempt provider role selection | Rejected — verification documents required | `ScholarshipProvider_submission_blocked_when_no_verification_documents` |
| TC-05 | Profile | Student | Update profile persists and validates | Authenticated | Submit valid profile update; then submit invalid fields | Valid update persists; invalid fields return validation errors | `UpdateProfileCommand*Tests` |
| TC-06 | Scholarship Discovery | Student | Search/filter local scholarships | ≥1 Open scholarship exists | Search by keyword + country + level with pagination | Only matching, Open, non-deleted results; correct order + page size | **Gap** — no current handler test |
| TC-07 | Scholarship Discovery | Student | Bookmark then list bookmarks | Authenticated; scholarship exists | Bookmark a scholarship; open bookmarks | Bookmark appears newest-first; localised (AR) when language=AR | `ScholarshipBookmarksAndFeaturedTests` |
| TC-08 | Local Scholarship Mgmt | Scholarship Provider | Create → publish → auto-close scholarship | Provider approved | Create scholarship, publish, advance past deadline | New scholarship defaults Draft; publishes Open; auto-close job closes after deadline | `ScholarshipTests`, `ScholarshipAutoCloseJobTests` |
| TC-09 | External Scholarship | Student | Track an external scholarship | External listing exists | Create intending application with valid tracking URL | Intending application created; invalid tracking URL rejected; duplicate active → conflict | `ExternalIntentCommandHandlerTests` |
| TC-10 | Application Submission | Student | Submit and resume application drafts | Open scholarship; authenticated | Start application; re-open; submit | Draft created once (resumed, not duplicated); valid status transitions enforced; terminal → read-only | `CreateApplicationCommandHandlerTests` (unit + integration) |
| TC-11 | Application Responding | Scholarship Provider | Respond to a submitted application (free path) | Submitted application on provider's scholarship; payments disabled | Start responding; move to In Assessment; complete | Free path skips payment and lands in Pending/In Assessment; own-scholarship & closed rejected; idempotent on double-click | `StartScholarshipProviderReviewRequestCommandHandlerTests` |
| TC-12 | Consultant Booking (no payment) | Student ↔ Consultant | Request → accept → no-show lifecycle | Admin-approved consultant with availability | Student requests slot; consultant accepts; mark no-show | Booking created (201) and confirmed (204); authorisation matrix enforced; no-show recorded | Booking endpoint integration tests |
| TC-13 | Community | Student / Consultant | Post creation permissions + tags | Authenticated users of each role | Student creates post with 6 tags; consultant/provider attempt to post | Student post succeeds with tags normalised and capped at 5; consultant/provider rejected | `CreatePostCommandHandlerTests`, `TagInput.test.tsx` |
| TC-14 | Messaging | Two students | Send message with block enforcement + real-time delivery | Both active; contact relationship | A sends to B (200); B blocks A; A resends (409); real-time receipt on B | Correct 200/409 per block state; message delivered over SignalR to online recipient | `BlockEnforcementIntegrationTests`; **real-time gap** |
| TC-15 | Resources Hub | Author / Admin | Resource publish workflow | Author has bio; admin available | Submit complete draft (→pending); admin approves (→published); reject returns to draft | Workflow transitions enforced; incomplete draft / missing bio blocked; admin submit publishes directly | `ResourcePublishWorkflowTests` |
| TC-16 | AI Advisory | Student | Chatbot redacts PII before processing | AI enabled; within cost budget | Send message containing PII; send over-budget request | PII redacted to tokens; empty/over-long rejected; cost gate blocks when budget exceeded | `AskChatbotRedactionTests`, `AiCostGateTests` |
| TC-17 | Dashboards / Role-based | Each role | Route guards + role-scoped dashboard | Unauthenticated + each authenticated role | Hit role routes unauthenticated; then as each role | Unauthenticated redirected to `/login`; each role sees only its dashboard data | E2E `auth.spec.ts` (PB-001 / T-018); **dashboard-handler gap** |
| TC-18 | Notifications | Student | Preference matrix controls delivery | Authenticated | Disable a channel; trigger a notification; re-enable | Disabled channel suppressed; others delivered; email dispatch idempotent | `NotificationPreferencesTests`, `EmailDeliveryIntegrationTests` |
| TC-19 | Admin Oversight | Admin | Approve onboarding + manage users | Admin; pending onboarding request | Approve provider onboarding; change a user role; set status; flag low-rated provider | Onboarding approved; role/status changes persisted + audited; low-rated flag surfaced; at-risk chip reflects flag | `Admin/*`, `AtRiskUserChipTests` |
| TC-20 | Security / Privacy / Audit | Student / System | Field encryption + GDPR export/delete | Sensitive fields stored; authenticated user | Round-trip encrypt/decrypt; tamper ciphertext; request data export then deletion | Round-trip returns plaintext; tamper rejected; export covers all related tables + writes audit entry (idempotent); delete removes personal data | `FieldEncryptionTests`, `DataExportJobTests`, `DataDeleteJobTests` |

---

## 8. Testing Results Summary

> **Executed** rows carry real pass/fail. **Code-inspection-only** rows report the *number of defined tests* discovered; their Passed/Failed/Skipped are shown as `—` because the suites were **not run** in this environment. `[Theory]` methods expand to more cases at runtime, so backend "Total" is a **lower bound** on executable cases.

| Test Category | Total Tests | Passed | Failed | Skipped | Notes |
|---------------|------------|--------|--------|---------|-------|
| Frontend unit (Vitest) | 17 | **17** | **0** | 0 | **Executed** — 5 files, exit 0, ~3.0 s |
| Backend unit (xUnit) | ≈696 methods (~111 classes) | — | — | — | **Code inspection only** — not executed (no .NET 10 SDK; download egress-blocked). Includes removed-scope payment/profit-share/financial tests that should be excluded from evaluation |
| Backend integration (xUnit + Testcontainers) | ≈35 methods | — | — | — | **Code inspection only** — not executed (needs Docker + SDK). 2 integration files contain factories with **0 tests** |
| Frontend E2E (Playwright) | ≈61 `test()` declarations across 14 specs (more at runtime via parametrised loops) | — | — | ~23 `skip`/`fixme` + credential-gated self-skips | **Not executed** — browser version mismatch + egress-blocked download; suite targets a deployed URL with seeded creds |
| **In-scope executed evidence** | **17** | **17** | **0** | **0** | Only the frontend Vitest suite was actually executed here |

### Removed-scope tests present in the repository (excluded from the evaluation above)

| Suite | Approx. count | Location |
|-------|--------------|----------|
| Payments (Stripe intents, Connect, refunds, payouts, webhooks) | ~45 unit methods | `UnitTests/Payments/` |
| Profit share (calculator, resolver, config) | ~18 unit methods | `UnitTests/ProfitShare/` |
| Financial config (calculator, rule resolver) | ~16 unit methods | `UnitTests/FinancialConfig/` |
| Payment-coupled (booking refund/sync, provider-review payment/refund, review-fee validators) | additional methods embedded in booking, provider-review, and scholarship suites | various |
| Payments integration | ~12 methods | `IntegrationTests/Payments/PaymentsIntegrationTests.cs` |
| Payments E2E | 3 (2 skipped) | `client/src/test/e2e/payments.spec.ts` |

---

### Appendix A — Exact commands & environment facts used for this report

- `server/global.json` → SDK `10.0.201`, `rollForward: latestFeature`.
- `dotnet` not on PATH; install blocked: `builds.dotnet.microsoft.com` → HTTP 403 (egress policy).
- `docker` present but daemon not running → Testcontainers unavailable.
- Node `v22.22.2`, npm `10.9.7`; `registry.npmjs.org` reachable.
- `client/` — `npm install` OK; `npm test` → **`Test Files 5 passed (5) / Tests 17 passed (17)`**.
- Playwright: pre-installed browser `chromium_headless_shell-1194`; project requires `-1217`; `cdn.playwright.dev` → HTTP 403.
- No committed test-result artifacts (`.trx`, `coverage.cobertura.xml`, `junit.xml`, `lcov.info`) found in the repository.

*End of testing evidence report. This document is evidence only and is not Chapter 5.*
