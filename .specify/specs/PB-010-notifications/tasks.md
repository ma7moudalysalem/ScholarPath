# PB-010 — Tasks

**Owner**: @Madiha6776  •  **Est**: 41 pts  •  **Iteration**: 3

## Backend
- [ ] T-001 — `Notification` + `NotificationPreference` entities
- [ ] T-002 — `NotificationDispatcher` service (persist + hub broadcast + email queue)
- [ ] T-003 — Domain-event subscribers mapping events → notification types (covers 15+ types)
- [ ] T-004 — `MarkReadCommand` + `MarkAllReadCommand` + `SendBroadcastCommand`
- [ ] T-005 — Preference management endpoints
- [ ] T-006 — `IEmailService` MailKit + SendGrid adapters; template renderer (Razor or Handlebars)
- [ ] T-007 — Unit + integration tests

## Frontend
- [ ] T-008 — `NotificationBell` in header (unread count badge)
- [ ] T-009 — `NotificationList` popover + infinite scroll
- [ ] T-010 — `NotificationPreferences` page
- [ ] T-011 — Admin `BroadcastComposer` in admin dashboard
- [ ] T-012 — SignalR client listening on `NotificationHub`
- [ ] T-013 — Arabic copy for every notification template (EN+AR)

## QA
- [ ] T-014 — E2E: trigger an event → notification appears in bell + email in MailHog
- [ ] T-015 — E2E: preferences off → no in-app delivery for that type

## Done criteria
15+ notification types wired; real-time + email both working in MailHog dev.
