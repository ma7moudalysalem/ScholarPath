# PB-013 — Tasks

**Owner**: @norra-mmhamed  •  **Est**: 39 pts  •  **Iteration**: 2

## Backend
- [ ] T-001 — `Payment`, `Payout`, `StripeWebhookEvent` entities + configs
- [ ] T-002 — `StripeService` wrapper implementing `IStripeService`
- [ ] T-003 — `CreatePaymentIntentCommand` (capture mode per payment type)
- [ ] T-004 — `CapturePaymentIntentCommand` (profit share calc in same transaction)
- [ ] T-005 — `RefundPaymentCommand` (partial refund support)
- [ ] T-006 — `HandleStripeWebhookCommand` + `StripeWebhookProcessor` (signature + idempotency)
- [ ] T-007 — `CreateConnectAccountCommand` returning onboarding link
- [ ] T-008 — `StripePayoutJob` (nightly Hangfire)
- [ ] T-009 — Email receipt via `IEmailService` on capture
- [ ] T-010 — Unit + integration tests (idempotency, signature verification, webhook replays)

## Frontend
- [ ] T-011 — Stripe Elements component for checkout
- [ ] T-012 — `PayoutSettings.tsx` Connect onboarding launcher
- [ ] T-013 — `Receipts.tsx` history view
- [ ] T-014 — Arabic copy review

## QA
- [ ] T-015 — E2E: book consultant with test card → accept → capture succeeds → receipt emailed
- [ ] T-016 — E2E: webhook replay (send same signed payload twice → second is noop)

## Done criteria
Test card 4242…: hold → capture → refund all green. Webhook idempotency verified under replay.
