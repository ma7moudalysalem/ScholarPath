# PB-013 — Payment Processing and Settlement

**Owner**: @norra-mmhamed • **Priority**: Essential • **Iteration**: 2 • **Est**: 39 pts

## Problem statement

Central payment module integrating Stripe for all money flows on the platform. Consultants onboard via Stripe Connect; Companies and Consultants are payees. Students pay via PaymentIntents with capture-later semantics for Consultant bookings and immediate capture for Company reviews. Profit share (PB-014) deducted per transaction. All events idempotent via webhook signatures + stored event log.

## User stories

US-143 .. US-152

## Functional requirements

FR-183 .. FR-200

## Acceptance criteria

1. **PaymentIntent creation** — `POST /api/payments/intent` returns `{clientSecret}` for Stripe Elements. Server attaches metadata (`bookingId` or `applicationId`), sets `capture_method: manual` for bookings and `automatic` for reviews.
2. **Capture** — `POST /api/payments/intent/{id}/capture` finalizes a held intent (used when Consultant accepts booking). Atomically updates `Payment` row + deducts profit share.
3. **Refund** — `POST /api/payments/intent/{id}/refund` with amount (partial supported). Updates `Payment.RefundedAmount` + `RefundReason`.
4. **Payout (Connect)** — `POST /api/payments/connect/onboard` generates Stripe Connect onboarding link for Consultants. Consultants paid out automatically after each captured payment (minus platform share).
5. **Webhooks** — `POST /api/webhooks/stripe` verifies signature, persists `StripeWebhookEvent` (unique on `event.id` — idempotent), dispatches to internal handler.
6. **Payment record** — `Payment { type, amount, currency, status, stripeIntentId, idempotencyKey, metadata, profitShareAmount, payeeAmount, payeeId, createdAt, capturedAt, refundedAt }`.
7. **Receipts** — Emailed on successful capture; viewable at `/billing/receipts/{id}`.
8. **All currencies USD in v1**; multi-currency deferred.

## Non-goals

- ACH / bank transfer direct (Stripe only)
- Alternative payment methods (Apple Pay / Google Pay) — Stripe Elements handles these automatically
- Multi-currency FX conversion
