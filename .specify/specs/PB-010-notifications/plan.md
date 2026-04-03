# PB-010 — Implementation Plan

## Domain
- `Notification` (recipient, type, title, body, deepLink, isRead, channel, priority, metadataJson)
- `NotificationPreference` (per user per type per channel)
- Enum `NotificationType` — large catalog (ApplicationSubmitted, ApplicationStatusChanged, BookingRequested, BookingConfirmed, BookingCancelled, PaymentSuccess, PaymentRefund, CompanyRatingReceived, AdminApproval, Broadcast, etc.)

## Application (`server/src/ScholarPath.Application/Notifications/`)
- Commands: `DispatchNotificationCommand` (internal; called by event handlers), `MarkReadCommand`, `MarkAllReadCommand`, `SendBroadcastCommand` (Admin)
- Queries: `GetMyNotificationsQuery`, `GetUnreadCountQuery`, `GetMyPreferencesQuery`
- Event handlers: subscribe to all domain events → map to notification types → dispatch

## Infrastructure
- `NotificationDispatcher` service — persists row + publishes to `NotificationHub` + enqueues email
- `IEmailService` implementations (MailKit + SendGrid)
- Hangfire recurring job for any retries

## API
- `GET /api/notifications/me`
- `PATCH /api/notifications/{id}/read`
- `POST /api/notifications/mark-all-read`
- `GET /api/notifications/preferences`
- `PATCH /api/notifications/preferences`
- `POST /api/notifications/broadcast` (Admin)

## Frontend
- `NotificationBell` component in header (shadcn popover)
- `NotificationList` with infinite scroll
- `NotificationPreferences` page
- Admin `BroadcastComposer`

## Tests
- Unit: event → notification mapper
- Integration: event dispatch → hub broadcast + email queued
- E2E: submit app → receive notification in bell

## Dependencies
PB-001; consumed by PB-003..PB-014
