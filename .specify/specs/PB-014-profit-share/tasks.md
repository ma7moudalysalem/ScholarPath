# PB-014 — Tasks

**Owner**: @norra-mmhamed  •  **Est**: 22 pts  •  **Iteration**: 3
**Status**: ✅ backend + frontend + unit tests shipped; integration tests and E2E pending.

## Backend
- [x] T-001 — `ProfitShareConfig` entity + unique-active constraint *(Domain entity + EF config; enforced in SetProfitShareConfigCommandHandler)*
- [x] T-002 — `SetProfitShareConfigCommand` (closes previous active config, opens new) *(`ProfitShare/Commands/SetProfitShareConfig/SetProfitShareConfigCommand.cs`)*
- [x] T-003 — `GetActiveProfitShareConfigsQuery` *(`ProfitShare/Queries/GetActiveProfitShareConfigs/GetActiveProfitShareConfigsQuery.cs`)*
- [x] T-004 — `ProfitShareCalculator` (pure function, unit-tested) + `ProfitShareConfigResolver` *(`ProfitShare/ProfitShareCalculator.cs` + `ProfitShareConfigResolver.cs`)*
- [x] T-005 — Integrated into `CapturePaymentIntentCommand` (PB-013) via `ProfitShareConfigResolver`
- [x] T-006 — `GetProfitShareAnalyticsQuery` aggregations *(`ProfitShare/Queries/GetProfitShareAnalytics/GetProfitShareAnalyticsQuery.cs`)*
- [x] T-007 — `GetProfitShareHistoryQuery` *(`ProfitShare/Queries/GetProfitShareHistory/GetProfitShareHistoryQuery.cs`)* — audit trail via EF history entries
- [x] T-008 — Unit tests: `ProfitShareCalculatorTests` + `ProfitShareConfigResolverTests` + `SetProfitShareConfigCommandHandlerTests` *(`tests/ScholarPath.UnitTests/ProfitShare/` — 3 test files)*

## Frontend
- [x] T-009 — Admin `AdminProfitShare.tsx` — history table + update form *(`pages/admin/AdminProfitShare.tsx`)*
- [ ] T-010 — Revenue widget in `AdminDashboard` *(not yet wired into AdminDashboard stats)*
- [x] T-011 — Arabic copy *(covered in existing AR locale files)*

## Done criteria
- [x] Rounding math exact (cents, no float) — ProfitShareCalculator unit-tested.
- [x] Audit log entry on every update — history stored via EF.
- [ ] Analytics charts render correctly in AdminDashboard revenue widget.
