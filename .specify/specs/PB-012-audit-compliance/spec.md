# PB-012 — Audit, Compliance, System Integrity

**Owner**: @ma7moudalysalem • **Priority**: High • **Iteration**: 4 • **Est**: 15 pts

## Problem statement

Every security-relevant and business-relevant action must produce an audit trail. Users can request data export / deletion (GDPR-style). System integrity checks guard against orphaned records and inconsistent state.

## User stories

US-139 .. US-142

## Functional requirements

FR-177 .. FR-182

## Acceptance criteria

1. **Audit log** — Every: auth event, admin action, payment event, moderation, profile update, role change. Stored in `AuditLog { actorId, action, targetType, targetId, beforeJson, afterJson, ipAddress, userAgent, occurredAt }`. Immutable (no update/delete API).
2. **Audit writer** — `IAuditService.WriteAsync(...)` called from all relevant handlers via a MediatR pipeline behavior when handler is decorated `[Auditable]`.
3. **Data export** — User requests `POST /api/users/me/data-export` → Hangfire job generates ZIP of user data (profile, applications, bookings, messages) → email download link.
4. **Data delete** — User requests `POST /api/users/me/data-delete` → 30-day cooling period → Hangfire job soft-deletes user + related PII (preserving aggregate analytics with anonymized user_id).
5. **Integrity checks** — Daily Hangfire job scans for orphan rows (e.g., `Payment` with missing booking), alerts admin.

## Non-goals

- Full GDPR DPO tooling (v2)
- Auditable data downloads for admins other than via logs (v2)
