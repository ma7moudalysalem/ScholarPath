# PB-012 — Tasks

**Owner**: @ma7moudalysalem  •  **Est**: 15 pts  •  **Iteration**: 4

## Backend
- [ ] T-001 — `AuditLog` + `UserDataRequest` entities
- [ ] T-002 — `IAuditService` + MediatR `AuditBehavior` pipeline behavior
- [ ] T-003 — Mark relevant commands with `[Auditable]` attribute (auth, admin, payments, moderation)
- [ ] T-004 — `RequestDataExportCommand` + Hangfire `DataExportJob`
- [ ] T-005 — `RequestDataDeleteCommand` + 30-day delayed Hangfire job + cancel command
- [ ] T-006 — Daily `IntegrityCheckJob` (orphan detection)
- [ ] T-007 — Admin `GetAuditLogQuery` with pagination + filters
- [ ] T-008 — Unit + integration tests

## Frontend
- [ ] T-009 — Student `DataPrivacy.tsx` settings page
- [ ] T-010 — Admin `AuditLog.tsx` viewer (reuses PB-011 layout)
- [ ] T-011 — Arabic copy review

## Done criteria
Audit pipeline non-invasive; export/delete flows work end-to-end in dev with MailHog.
