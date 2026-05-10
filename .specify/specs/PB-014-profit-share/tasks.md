# PB-014 — Tasks

**Owner**: @norra-mmhamed  •  **Est**: 22 pts  •  **Iteration**: 3

## Backend
- [ ] T-001 — `ProfitShareConfig` entity + unique-active constraint
- [ ] T-002 — `SetProfitShareConfigCommand` (closes previous, opens new)
- [ ] T-003 — `GetActiveProfitShareConfigQuery`
- [ ] T-004 — `ProfitShareCalculatorService` (pure function, unit-tested)
- [ ] T-005 — Integrate into `CapturePaymentIntentCommand` (PB-013)
- [ ] T-006 — `GetProfitShareAnalyticsQuery` aggregations
- [ ] T-007 — Audit config changes via `[Auditable]` attribute
- [ ] T-008 — Unit + integration tests

## Frontend
- [ ] T-009 — Admin `ProfitShareConfig.tsx` — history + update form
- [ ] T-010 — Revenue widget in `AdminDashboard`
- [ ] T-011 — Arabic copy

## Done criteria
Rounding math exact (cents, no float); audit log entry on every update; analytics charts render correctly.
