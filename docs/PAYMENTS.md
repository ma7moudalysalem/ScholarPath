# Payments

Stripe powers all money flows. Two payment types; both flow through the shared `Payment` ledger.

| Type                  | Trigger                             | Capture           | Payee            |
|-----------------------|-------------------------------------|-------------------|------------------|
| ConsultantBooking     | Student requests a consultant slot  | On accept (manual)| Consultant (Connect) |
| CompanyReview         | Student submits an in-app application to a listing with a review fee | On submit (automatic) | Company |

## Consultant booking lifecycle

```
Student clicks Book
   │
   ▼
POST /api/payments/intent        (capture_method=manual)
   │  returns { clientSecret }
   ▼
Stripe Elements collects card
   │
   ▼
stripe.confirmPayment            (authorization held on card)
   │
   ▼
POST /api/consultants/{id}/book  (bookingId + paymentIntentId)
   │  Booking row created in Requested state
   ▼
─── 24-hour window ──────────────────────────
   │                                │
   │ Consultant ACCEPTS             │ Consultant REJECTS or TIMES OUT
   ▼                                ▼
POST /api/bookings/{id}/accept   POST /api/bookings/{id}/reject or SessionExpiryJob tick
   ├─ CapturePaymentIntentAsync   ├─ CancelPaymentIntentAsync (releases hold)
   ├─ Booking → Confirmed         ├─ Booking → Rejected/Expired
   ├─ meeting URL generated       ├─ notify student
   ├─ notify both parties         │
   └─ Payment.Status = Captured   └─ Payment.Status = Cancelled
```

## Refund matrix (FR-085..FR-091)

| Event                                                      | Refund |
|------------------------------------------------------------|--------|
| Student cancels **before** consultant acceptance           | 100%   |
| Student cancels **>24h before** accepted session           | 100%   |
| Student cancels **<24h before** accepted session           | 50%    |
| Consultant cancels after acceptance                        | 100%   |
| Consultant marked no-show                                  | 100%   |
| Student marked no-show                                     | 0%     |

Implemented pure-function `RefundCalculatorService` with a state/reason input. Unit-test the whole matrix.

## Company review lifecycle

```
Student submits application
   │
   ▼
POST /api/payments/intent        (capture_method=automatic)
   │  instant capture — no escrow
   ▼
Payment captured, funds credited
   ↓
CompanyReviewTimeoutRefundJob (daily)
   └─ if 14 days pass with no Company review → full refund
```

## Profit share (PB-014)

Every successful capture runs through `ProfitShareCalculatorService`:

```
profitShareAmountCents = round(amountCents * activeConfig.Percentage)
payeeAmountCents       = amountCents - profitShareAmountCents
```

Snapshot stored on the `Payment` row (never re-derived). Default: 10% consultant, 15% company — configurable per payment type via Admin UI.

## Stripe Connect (consultant payouts)

1. Consultant clicks "Connect account" in settings.
2. API: `POST /api/payments/connect/onboard` → Stripe hosted onboarding URL.
3. After verification, Stripe sends `account.updated` webhook → mark consultant `ConsultantVerifiedAt`.
4. `StripePayoutJob` runs nightly and initiates payouts for captured payments.

## Webhooks — idempotency is law

Endpoint: `POST /api/webhooks/stripe` (public but signature-verified).

```
incoming payload
   ├─ verify signature (WebhookSecret from options)
   ├─ parse → StripeEventId
   ├─ check StripeWebhookEvents table (unique index on StripeEventId)
   │   └─ if exists → return 200 (noop — already processed)
   ├─ insert StripeWebhookEvent row (IsProcessed=false)
   ├─ dispatch to handler per event type
   │   └─ on success: IsProcessed=true, ProcessedAt=now
   └─ return 200
```

Any handler failure lets the event be retried on next Stripe delivery (event keeps `IsProcessed=false`).

## Currency

v1 is USD-only. `Payment` stores amounts in **cents** (integer) to dodge float rounding. Multi-currency is a v2 decision.

## PCI scope

ScholarPath never sees raw card data — Stripe Elements keeps it in Stripe's iframe. Our scope is SAQ-A.

## Testing checklist

- [ ] Happy path: hold → accept → capture → payout
- [ ] Reject path: hold → reject → release
- [ ] Expire path: hold → 24h → SessionExpiryJob → release
- [ ] Cancel matrix (6 rows)
- [ ] Webhook replay (same StripeEventId twice → second is noop)
- [ ] Connect onboarding: incomplete → `account.updated` → complete
- [ ] Profit share rounding (5.555 → 5.56, 0.0001 → 0)

Use Stripe test mode (`4242 4242 4242 4242` any CVC, future expiry). Integration tests run against Stripe's test API.
