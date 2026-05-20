# PB-010 — Tasks

**Owner**: @Madiha6776  •  **Est**: 41 pts  •  **Iteration**: 3
**Status**: ✅ backend + full frontend shipped (including NotificationPreferences page); E2E pending staging.

## Backend
- [x] T-001 — `Notification` + `NotificationPreference` entities *(`Domain/Entities/Notifications.cs`)*
- [x] T-002 — `NotificationDispatcher` service (persist + hub broadcast + email queue) *(`Infrastructure/Services/NotificationDispatcher.cs` + `INotificationDispatcher` interface)*
- [x] T-003 — Domain-event subscribers mapping events → notification types *(`NotificationCatalog.cs` + `INotificationCatalog`; `ChatMessageNotificationEventHandler`; `BookingNotificationEventHandlers`; `NotificationParams.cs`)*
- [x] T-004 — `MarkAsReadCommand` + `MarkAllAsReadCommand` + `SendBroadcastCommand` *(`Notifications/Commands/MarkAsRead/` + `MarkAllAsRead/`; `Admin/Commands/SendBroadcast/`)*
- [x] T-005 — Preference management endpoints — `UpdateNotificationPreferenceCommand` + `GetNotificationPreferencesQuery` *(`Notifications/Commands/UpdateNotificationPreference/` + `Notifications/Queries/GetNotificationPreferences/`)*
- [x] T-006 — `IEmailService` MailKit adapter *(`Infrastructure/Services/MailKitEmailService.cs`)*
- [x] T-007 — Unit tests: `NotificationCatalogTests`, `NotificationDispatcherTests`, `NotificationPreferencesTests` *(`tests/ScholarPath.UnitTests/Notifications/` — 3 test files)*

## Frontend
- [x] T-008 — `NotificationBell` in header (unread count badge + link to /notifications) — embedded in `AuthenticatedLayout.tsx` (polled via `notificationsApi.unreadCount()`; badge shows count when > 0)
- [x] T-009 — `NotificationList` page + load-more *(`pages/notifications/Notifications.tsx` — paginated with `pageSize` expansion, mark-read + mark-all-read)*
- [x] T-010 — `NotificationPreferences` page *(`pages/notifications/NotificationPreferences.tsx` — toggle matrix grouped by category, optimistic updates; route `/notifications/preferences`; link from Notifications page)*
- [x] T-011 — Admin `BroadcastComposer` in admin portal *(`pages/admin/BroadcastComposer.tsx`)*
- [x] T-012 — SignalR client listening on `NotificationHub` *(`hooks/useNotificationHub.ts` — toast on incoming notifications; invalidates unread count)*
- [x] T-013 — Arabic copy for every notification template *(`locales/ar/notifications.json` — full AR)*

## QA
- [x] T-014 — E2E: trigger an event → notification appears in bell + email in MailHog *(`client/src/test/e2e/notifications.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` set)*
- [x] T-015 — E2E: preferences off → no in-app delivery for that type *(`client/src/test/e2e/notifications.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` set)*

## Done criteria
- [x] 15+ notification types wired; real-time (SignalR) working.
- [x] NotificationPreferences page (T-010) wired — toggle matrix, optimistic updates, routed at `/notifications/preferences`.
- [ ] Email delivery proven in MailHog dev environment. *(E2E spec includes bell+email check; MailHog must be running in dev to verify email side)*
- [ ] E2E green in staging. *(spec written — `notifications.spec.ts`; needs staging credentials to run)*
