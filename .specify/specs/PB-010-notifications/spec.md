# PB-010 — Notifications

**Owner**: @Madiha6776 • **Priority**: High • **Iteration**: 3 • **Est**: 41 pts

## Problem statement

Users need timely notifications about application status, booking events, review outcomes, admin broadcasts, and payment receipts. Delivery is multi-channel (in-app via SignalR + email) with user preferences per channel.

## User stories

US-103 .. US-117

## Functional requirements

FR-138 .. FR-152

## Acceptance criteria

1. **Notification entity** — typed, deep-linked, read/unread, channel (in-app/email/push-future), priority.
2. **Channels** — In-app via `NotificationHub`; email via `IEmailService` (MailKit local, SendGrid prod).
3. **Event-driven** — Consumes domain events (`ApplicationSubmittedEvent`, `BookingConfirmedEvent`, etc.) and produces notification rows + dispatches.
4. **Preferences** — User can toggle per-channel preferences for each notification type (with sensible defaults).
5. **Admin broadcast** — Admin sends to all/filtered users; creates N rows.
6. **Read/unread** — `PATCH /api/notifications/{id}/read`; bell shows unread count.
7. **Digest (deferred)** — Daily digest email for unread items — v2.
8. **Idempotency** — Each notification keyed by `(userId, eventId, channel)` to prevent duplicate on event replay.

## Non-goals

- SMS/push notifications (v2)
- Rich notification composition (v2)
- Notification templates editor (admin hardcoded in v1)
