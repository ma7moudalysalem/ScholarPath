# PB-014 — Implementation Plan

## Domain
- `ProfitShareConfig` (one active per payment type)
- Extends `Payment` with `ProfitShareAmount`, `PayeeAmount` snapshots

## Application (`server/src/ScholarPath.Application/ProfitShare/`)
- Commands: `SetProfitShareConfigCommand` (Admin; closes previous active, opens new)
- Queries: `GetActiveProfitShareConfigQuery`, `GetProfitShareHistoryQuery`, `GetProfitShareAnalyticsQuery`
- Service: `ProfitShareCalculatorService` (pure function consumed by PB-013 capture)

## Infrastructure
- Database constraint: per-payment-type uniqueness on `(paymentType, effectiveTo IS NULL)`

## API
- `GET  /api/admin/profit-share/active`
- `GET  /api/admin/profit-share/history`
- `PUT  /api/admin/profit-share/{paymentType}` (set new)
- `GET  /api/admin/profit-share/analytics?from=&to=`

## Frontend (Admin)
- `ProfitShareConfig.tsx` — history timeline + "Update rate" action
- Included in `AdminDashboard` revenue card

## Tests
- Unit: `ProfitShareCalculatorService` (rounding, boundary cases, config not found)
- Integration: update config → subsequent capture uses new rate; previous payments unchanged
- Integration: audit log entry on config change

## Dependencies
PB-013 (consumes this service), PB-011 (admin UI), PB-012 (audit)
