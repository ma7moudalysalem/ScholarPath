# PB-012 — Implementation Plan

## Domain
- `AuditLog` (append-only, indexed on actor, target, timestamp)
- `UserDataRequest { type: Export|Delete, status, requestedAt, completedAt, downloadUrl? }`

## Application (`server/src/ScholarPath.Application/Audit/`)
- Commands: `RequestDataExportCommand`, `RequestDataDeleteCommand`, `CancelDataDeleteCommand`
- Queries: `GetAuditLogQuery` (Admin), `GetMyDataRequestsQuery`
- Services: `IAuditService` with `WriteAsync(...)`
- Pipeline behavior: `AuditBehavior<TRequest, TResponse>` — writes audit entry after handler success for `[Auditable]` requests

## Infrastructure
- Hangfire jobs: `DataExportJob`, `DataDeleteJob` (30-day delay), `IntegrityCheckJob` (daily)
- `AuditLog` write-only via `AuditService`

## API
- `POST /api/users/me/data-export`
- `POST /api/users/me/data-delete`
- `POST /api/users/me/data-delete/cancel`
- `GET /api/users/me/data-requests`
- `GET /api/admin/audit-log` (paginated)

## Frontend
- Student: `DataPrivacy.tsx` in settings
- Admin: `AuditLog.tsx` viewer with filters

## Tests
- Unit: `AuditBehavior` writes entry on success, skips on failure
- Integration: export request → job runs → download link issued
- Integration: delete request → 30d later → soft-deleted

## Dependencies
PB-001 (user identity), PB-010 (completion emails)
