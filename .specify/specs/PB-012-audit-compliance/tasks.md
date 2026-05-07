# PB-012 — Tasks

**Owner**: @ma7moudalysalem  •  **Est**: 15 pts  •  **Iteration**: 4
**Status**: ✅ all backend + frontend shipped; closed pending team QA sign-off.

## Backend
- [x] T-001 — `AuditLog` + `UserDataRequest` entities  *(shipped with initial migration, `16d9046`)*
- [x] T-002 — `IAuditService` + MediatR `AuditBehavior` pipeline behavior  *(`e5b20c6`)*
- [x] T-003 — Mark relevant commands with `[Auditable]` attribute (auth, admin, payments, moderation)  *(admin commands in `2c95359`, `d5c3da5`, `0fbade7`; other modules inherit automatically)*
- [x] T-004 — `RequestDataExportCommand` + Hangfire `DataExportJob`  *(`1980978`)*
- [x] T-005 — `RequestDataDeleteCommand` + 30-day delayed Hangfire job + cancel command  *(`1980978`)*
- [x] T-006 — Daily `IntegrityCheckJob` (orphan detection)  *(`dc9f94d`)*
- [x] T-007 — Admin `GetAuditLogQuery` with pagination + filters  *(`dc9f94d`)*
- [x] T-008 — Unit + integration tests  *(`1de65ed` — 4 tests on AuditBehavior, part of 52 total green)*

## Frontend
- [x] T-009 — Student `DataPrivacy.tsx` settings page  *(`c39d53d`)*
- [x] T-010 — Admin `AuditLog.tsx` viewer (reuses PB-011 layout)  *(`aa790dd`)*
- [x] T-011 — Arabic copy review  *(full AR `privacy.json` + `admin.audit.*`)*

## Done criteria
- [x] Audit pipeline non-invasive (single attribute opt-in, never fails the command)
- [x] Export + delete flows end-to-end in dev with MailHog
- [x] 30-day delete cooling period with cancel option
