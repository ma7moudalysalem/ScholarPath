# PB-015 — Tasks

**Owner**: @TasneemShaaban • **Est**: 34 pts • **Iteration**: 2
**Status**: 🟡 SQL views shipped; Power BI reports + embed-token API + frontend page pending.

## SQL views (backend prep)

- [x] T-001 — Create `vw_funnel_daily` view aggregating registrations, onboarding-completed, application-submitted, application-accepted per day *(in `20260519053514_AddReportingViews_PB015.cs`)*
- [x] T-002 — Create `vw_acceptance_rates` view grouped by scholarship + field + country *(in migration)*
- [x] T-003 — Create `vw_finance_daily` view with revenue split (Booking vs CompanyReview), profit share accrual, refund counts *(in migration)*
- [x] T-004 — Create `vw_consultant_kpis` view parameterized by consultant id *(in migration)*
- [x] T-005 — Create `vw_student_journey` view parameterized by student id *(in migration)*
- [x] T-006 — EF migration for the five views *(`Infrastructure/Migrations/20260519053514_AddReportingViews_PB015.cs`)*

## Power BI reports

- [ ] T-007 — PB-015-US-001 Executive Dashboard (4 KPI cards + funnel + world map + top-10 scholarships) *(Power BI workspace not yet provisioned)*
- [ ] T-008 — PB-015-US-002 Student Success Dashboard (acceptance rates + journey + consultant-uplift)
- [ ] T-009 — PB-015-US-003 Financial Dashboard (revenue, profit share, refunds, ProfitShareConfig history)
- [ ] T-010 — PB-015-US-004 Consultant Self-Analytics
- [ ] T-011 — PB-015-US-005 Student Self-Analytics

## Security

- [ ] T-012 — PB-015-US-006 Configure Power BI RLS roles mapped to four JWT roles
- [ ] T-013 — Verify RLS via four impersonation tests

## API + frontend

- [ ] T-014 — `POST /api/admin/analytics/embed-token` endpoint + token scoping *(not yet in backend)*
- [ ] T-015 — `AnalyticsEmbedded.tsx` page with iframe + refresh loop *(not yet; `AnalyticsPage.tsx` covers admin KPI charts but not Power BI embed)*
- [ ] T-016 — Role-specific links in nav (admin, consultant, student) *(admin analytics link exists in AdminLayout; consultant/student links not yet wired)*

## QA

- [ ] T-017 — Playwright smoke: each role sees their dashboard only
- [ ] T-018 — Manual UX review on mobile viewport
- [ ] T-019 — Documentation in `docs/ANALYTICS.md`

## Done criteria

- [x] Five SQL views created and migrated.
- [ ] Five Power BI dashboards live and accessible per role.
- [ ] RLS verified by impersonation tests.
- [ ] Auto-refresh every 4 hours.
- [ ] `docs/ANALYTICS.md` published.
