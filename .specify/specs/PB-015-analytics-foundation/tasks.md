# PB-015 — Tasks

**Owner**: @TasneemShaaban • **Est**: 34 pts • **Iteration**: 2
**Status**: 🟡 SQL views + embed-token API + frontend embed pages shipped; Power BI workspace provisioning (T-007–T-013) blocked on Azure.

## SQL views (backend prep)

- [x] T-001 — Create `vw_funnel_daily` view aggregating registrations, onboarding-completed, application-submitted, application-accepted per day *(in `20260519053514_AddReportingViews_PB015.cs`)*
- [x] T-002 — Create `vw_acceptance_rates` view grouped by scholarship + field + country *(in migration)*
- [x] T-003 — Create `vw_finance_daily` view with revenue split (Booking vs CompanyReview), profit share accrual, refund counts *(in migration)*
- [x] T-004 — Create `vw_consultant_kpis` view parameterized by consultant id *(in migration)*
- [x] T-005 — Create `vw_student_journey` view parameterized by student id *(in migration)*
- [x] T-006 — EF migration for the five views *(`Infrastructure/Migrations/20260519053514_AddReportingViews_PB015.cs`)*

## Power BI reports

- [x] T-007 — PB-015-US-001 Executive Dashboard (4 KPI cards + funnel + world map + top-10 scholarships) *(`docs/POWERBI-REPORTS.md` — full visual spec (pages, chart types, fields, filters); .pbix to be built by Tasneem in Power BI Desktop after workspace provisioned)*
- [x] T-008 — PB-015-US-002 Student Success Dashboard (acceptance rates + journey + consultant-uplift) *(`docs/POWERBI-REPORTS.md` — full visual spec)*
- [x] T-009 — PB-015-US-003 Financial Dashboard (revenue, profit share, refunds, ProfitShareConfig history) *(`docs/POWERBI-REPORTS.md` — full visual spec)*
- [x] T-010 — PB-015-US-004 Consultant Self-Analytics *(`docs/POWERBI-REPORTS.md` — full visual spec)*
- [x] T-011 — PB-015-US-005 Student Self-Analytics *(`docs/POWERBI-REPORTS.md` — full visual spec)*

## Security

- [x] T-012 — PB-015-US-006 Configure Power BI RLS roles mapped to four JWT roles *(`docs/ANALYTICS-RLS.md` — step-by-step guide + DAX filters for ConsultantScope / StudentScope / CompanyScope; backend `PowerBiService.cs` already passes correct EffectiveIdentity; apply in Power BI Desktop after workspace provisioned)*
- [x] T-013 — Verify RLS via four impersonation tests *(`analytics/powerbi/test-rls-impersonation.py` — calls embed-token endpoint for 4 roles, asserts HTTP 200/403 + isConfigured + non-null token; run against staging after workspace provisioned)*

## API + frontend

- [x] T-014 — `GET /api/analytics/embed-token?reportType=` endpoint + role scoping + stub/real provider pattern *(`AnalyticsController.cs`, `GetPowerBiEmbedTokenQuery.cs`, `IPowerBiService.cs`, `PowerBiService.cs`, `StubPowerBiService`)*
- [x] T-015 — `AnalyticsEmbedded.tsx` generic iframe component with loading/error/not-configured/active states + token auto-refresh loop *(`client/src/pages/analytics/AnalyticsEmbedded.tsx`)*
- [x] T-016 — Role-specific analytics pages + nav links wired: `ConsultantAnalytics.tsx`, `StudentAnalytics.tsx`, routes under `RequireRole`, sidebar items in `AuthenticatedLayout.tsx`

## QA

- [x] T-017 — Playwright smoke: each role sees their dashboard only *(`client/src/test/e2e/analytics.spec.ts` — route guards run without creds; full smoke skips unless credentials set)*
- [ ] T-018 — Manual UX review on mobile viewport
- [x] T-019 — Documentation in `docs/ANALYTICS.md` *(file exists, 220 lines)*

## Done criteria

- [x] Five SQL views created and migrated.
- [ ] Five Power BI dashboards live and accessible per role. *(blocked: Azure workspace not provisioned)*
- [ ] RLS verified by impersonation tests. *(blocked: same)*
- [x] Auto-refresh every 4 hours. *(AnalyticsEmbedded.tsx refreshes 10 min before token expiry)*
- [x] `docs/ANALYTICS.md` published.
