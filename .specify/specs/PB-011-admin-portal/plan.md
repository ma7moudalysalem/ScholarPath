# PB-011 — Implementation Plan

## Application (`server/src/ScholarPath.Application/Admin/`)
Organized as sub-features mirroring sibling modules:
- `Admin/Users/` — Commands: `ActivateUserCommand`, `SuspendUserCommand`, `DeactivateUserCommand`, `DeleteUserCommand`, `ApproveUpgradeCommand`, `RejectUpgradeCommand`, `ApproveCompanyOnboardingCommand`, `ApproveConsultantOnboardingCommand`. Queries: `SearchUsersQuery`, `GetUpgradeQueueQuery`, `GetOnboardingQueueQuery`.
- `Admin/Content/` — moderation queries + commands delegating to respective modules (scholarships, articles, community).
- `Admin/Analytics/` — `GetPlatformOverviewQuery`, `GetUserGrowthQuery`, `GetRevenueQuery`, `GetAiUsageQuery`, `GetApplicationsMetricsQuery`.
- `Admin/Broadcast/` — `SendBroadcastCommand` (delegates to PB-010).
- Financial config (PB-014 owns implementation; admin just calls its endpoints).

## API (`AdminController.cs` with sub-controllers if needed)
Many endpoints; group by resource:
- `/api/admin/users/*`, `/api/admin/onboarding/*`, `/api/admin/upgrades/*`
- `/api/admin/content/scholarships/*`, `/api/admin/content/articles/*`, `/api/admin/content/community/*`
- `/api/admin/analytics/*`
- `/api/admin/broadcast`
- `/api/admin/settings/profit-share` (delegates to PB-014)

## Frontend (`pages/admin/`)
- `AdminDashboard.tsx` — KPI cards + charts
- `UsersAdmin.tsx`, `UpgradeQueue.tsx`, `OnboardingQueue.tsx`
- `ScholarshipsAdmin.tsx`, `ArticlesAdmin.tsx`, `CommunityModeration.tsx`
- `ProfitShareConfig.tsx` (PB-014)
- `AuditLog.tsx` (read-only; PB-012)
- `BroadcastComposer.tsx` (PB-010)
- `Analytics.tsx` with `Recharts`
- Use `AdminLayout.tsx` sidebar nav

## Tests
- Integration: approve upgrade → user role flips → audit row
- Integration: suspend user → subsequent login returns 403
- E2E: admin full flow (approve a consultant onboarding request)

## Dependencies
All other modules (this is the oversight layer)
