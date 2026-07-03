# Payments

Stripe powers all money flows. Two payment types; both flow through the shared `Payment` ledger.

| Type                  | Trigger                             | Capture           | Payee            |
|-----------------------|-------------------------------------|-------------------|------------------|
| ConsultantBooking     | Student requests a consultant slot  | On accept (manual)| Consultant (Connect) |
| ScholarshipProviderReview (review fee) | Student submits an in-app application to a listing with a review fee | **Manual hold → capture when the provider reviews** | ScholarshipProvider |

> **Shipped behavior (was mis-documented):** the review fee is **not** captured
> instantly on submit. Like a booking, the card is authorized (`PaymentStatus.Held`)
> when the application is submitted and captured only when the provider actually
> reviews it. If the provider never reviews within the SLA the hold is cancelled
> (no charge) or a captured payment is fully refunded — see the lifecycle below.

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

## Scholarship-provider review lifecycle

```
Student submits an in-app application to a listing with a review fee
   │
   ▼
POST /api/payments/intent        (capture_method=manual)
   │  authorization HELD on card — no charge yet (Payment.Status = Held)
   ▼
Provider reviews the application (accept / shortlist / reject decision)
   ├─ CaptureScholarshipProviderReviewPayment → Payment.Status = Captured
   │
   └─ Provider never reviews in time
        ↓
ScholarshipProviderReviewTimeoutRefundJob (daily)
   ├─ Held    → cancel the hold (no charge), Status = Cancelled
   └─ Captured→ full Stripe refund, Status = Refunded
```

**Timeout SLA (FR-068):** the job refunds when `Scholarship.Deadline + 14 days < now`.
This is deliberately keyed to the **scholarship deadline**, not the submission date —
a provider reviews applications only after the listing closes, so the 14-day
response window starts at the deadline. (An audit finding suggested keying it to the
submission date; that was a mis-read caused by the naming overlap described next —
the deadline-based rule matches FR-068 and is the intended behavior, so the job was
left unchanged.)

> ### Terminology — two different "review" concepts (do not confuse)
>
> - **`ScholarshipProviderReview` (review fee, above)** — the paid flow where a
>   student pays a fee for a provider to review their **in-app application**. It is
>   tied to an `Application` (`Payment.RelatedApplicationId`) and its timeout is
>   deadline-based (FR-068).
> - **`ScholarshipProviderReviewRequest`** — a separate direct "request a review"
>   entity with its own hold-then-capture lifecycle (`Start` → `ConfirmHold` →
>   `Accept`/`Reject`/`Expire` → `Complete`). Its pending window is keyed to the
>   **submission** date via `PendingExpiresAt = submittedAt + PendingTtlDays`.
> - **`ScholarshipProviderReview` rating** (community/ratings) — a *star rating* a
>   student leaves for a provider. **Carries no money.**
>
> These share a name prefix but are distinct; a v2 rename could disambiguate. When
> reading payment code, follow the `Payment.Type` / `RelatedApplicationId` /
> `RelatedBookingId` link, not the type name alone.

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

## No-show refunds & the clawback gap (DES-02)

No-show handling is intentionally asymmetric (FR-091/FR-193):

| Who is absent      | Outcome                                             |
|--------------------|-----------------------------------------------------|
| Consultant no-show | **Full refund** to the student; the consultant earns nothing — the payment split is zeroed (`ProfitShareAmountCents = PayeeAmountCents = 0`) so the payout job can never pay a consultant for a session they didn't attend. |
| Student no-show    | **No refund** — the session fee is forfeited.       |

Both the manual `MarkNoShow` path and the automated `MeetingNoShowSweepJob` refund
off the **captured `Payment.AmountCents`** (the source of truth), never a re-derived
`PriceUsd` — `PriceUsd` can drift (re-price, free-mode toggle, prior partial refund).

**Known limitation (v2):** there is no *clawback* if a consultant is paid out and the
student is only later refunded on a dispute. The mitigation today is that the no-show
and refund paths zero the split **before** payout, so the common cases can't overpay;
a genuine `CompensationClawback` ledger (reducing the consultant's next payout) is a
deferred v2 item.

## Free mode (payments.enabled master switch)

The `payments.enabled` platform setting puts the whole platform in **free mode**
(PB-005R/PB-006R): new bookings and review requests are created with a $0 effective
fee and skip Stripe entirely.

**Non-retroactive by design (DES-01):** toggling to free mode does **not** waive
commission on payments already captured before the toggle. Those keep their
`ProfitShareAmountCents` and are still paid out normally. To surface the exposure,
`UpdatePlatformSettingCommand` logs a warning listing the count / gross / commission
of captured (not-yet-paid-out) payments whenever an admin flips `payments.enabled`
true → false, so they can review those rows before the next payout. Whether a
retroactive waiver is intended is an open team decision (do not assume it).

## Data erasure vs financial retention (LEGAL-01 / FR-210)

A GDPR-style delete request **anonymizes** rather than hard-deletes a user who is
referenced by financial rows. Payment / refund / payout / audit records are retained
for tax and accounting; the `User`'s identifying fields are scrubbed (name →
"Deleted User", contact fields nulled). This keeps financial history intact and
prevents FK/read breakage on payment queries that still reference the user. Financial
records are therefore explicitly **exempt** from erasure — documented here and in
`docs/SRS.md` (FR-210).

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
