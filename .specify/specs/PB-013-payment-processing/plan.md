# PB-013 — Implementation Plan

## Domain
- `Payment` (central ledger row)
- `Payout` (Stripe Connect transfer)
- `StripeWebhookEvent` (idempotency + raw payload)
- Enum `PaymentType { ConsultantBooking, CompanyReview }`, `PaymentStatus { Pending, Held, Captured, Refunded, PartiallyRefunded, Failed }`

## Application (`server/src/ScholarPath.Application/Payments/`)
- Commands: `CreatePaymentIntentCommand`, `CapturePaymentIntentCommand`, `CancelPaymentIntentCommand`, `RefundPaymentCommand`, `CreateConnectAccountCommand`, `CreatePayoutCommand`, `HandleStripeWebhookCommand`
- Queries: `GetMyPaymentsQuery` (role-aware), `GetPayoutsQuery` (Consultant/Company), `GetPaymentDetailQuery`

## Infrastructure
- `Services/StripeService.cs` — thin wrapper on Stripe.net 51
- `Webhooks/StripeWebhookProcessor.cs` — signature verification + idempotency + dispatch
- Hangfire job `StripePayoutJob` — runs nightly to initiate scheduled payouts

## API (`PaymentsController.cs` + `WebhooksController.cs`)
- `POST /api/payments/intent`
- `POST /api/payments/intent/{id}/capture`
- `POST /api/payments/intent/{id}/refund`
- `POST /api/payments/connect/onboard` (Consultant/Company)
- `GET  /api/payments/me`
- `GET  /api/payments/{id}`
- `POST /api/webhooks/stripe` (public but signature-verified)

## Frontend
- Student: Stripe Elements integration on `BookingCheckout.tsx` + `ApplicationSubmit.tsx`
- Consultant/Company: `PayoutSettings.tsx` (Connect onboarding link)
- Billing history: `Receipts.tsx`

## Tests
- Unit: webhook signature verification; idempotency; profit-share calc
- Integration: Stripe test-mode (4242 card) — create intent → capture → refund
- E2E: book + pay with test card

## Dependencies
Shared by PB-005, PB-006, PB-014

## Risks
1. Webhook ordering — must be idempotent + state-machine safe
2. Connect account verification — Consultants can't be paid until verified
3. Currency rounding — always use cents (integer) in DB
