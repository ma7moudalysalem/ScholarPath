# PB-015 — Implementation Plan

## Data source
DirectQuery on production SQL Server. No new storage in this Epic — dashboards read from:
- `Users` + `UserProfiles` (identity, country, language)
- `ApplicationTrackers` + `ApplicationTrackerChildren` (funnel, decisions)
- `Payments` + `Payouts` + `ProfitShareConfigs` (finance)
- `ConsultantBookings` + `ConsultantReviews` (consultant self-view)
- `SavedScholarships`, `Scholarships`, `AiInteractions` (student self-view — read-only projection)

Dashboards query through five SQL views that ship as part of this Epic:
- `vw_funnel_daily` (registrations → onboarding → applications → accepted)
- `vw_acceptance_rates` (by scholarship, field, country)
- `vw_finance_daily` (revenue, profit share, refunds)
- `vw_consultant_kpis` (per consultant, filtered by `:userId`)
- `vw_student_journey` (per student, filtered by `:userId`)

## Application
- No new commands/queries — all SQL lives in DB views.
- New admin route `/admin/analytics` hosts an iframe to the embedded Power BI report.

## Infrastructure
- Azure subscription + Power BI Pro workspace (INFRA-US-001).
- AAD app registration + Service Principal for embed tokens (INFRA-US-002).
- Secrets in Azure Key Vault.
- Embed token minted server-side via `Microsoft.PowerBI.Api` NuGet package.

## API
- `POST /api/admin/analytics/embed-token` (Admin, SuperAdmin, Consultant, Student — request scoped to caller's role) → returns short-lived embed token.

## Frontend
- `client/src/pages/admin/AnalyticsEmbedded.tsx` — iframe + token refresh loop.
- Student / Consultant versions in their own dashboard pages (linked from role nav).

## Row-Level Security (RLS)
Power BI DAX role expressions:
- `[IsSameUser] = USERPRINCIPALNAME() = Users[Email]`
- Finance role is a named claim inside `Users[Role]` column, filtered by `Users[Role] = "Finance"`.
- Verified by four impersonation tests as part of acceptance.

## Tests
- Unit: embed token endpoint returns 403 for unauthenticated callers, scopes token to requesting role.
- Integration: student A cannot query student B's rows through the report.
- UI smoke (Playwright): authenticated admin visits `/admin/analytics` and the iframe loads.

## Dependencies
- PB-001 (JWT + role claims)
- INFRA (Azure + Power BI + Key Vault provisioned)
