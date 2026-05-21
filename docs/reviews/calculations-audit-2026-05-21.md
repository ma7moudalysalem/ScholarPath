# Booking + Payment calculations audit (2026-05-21)

Read-and-report audit of the four numbered areas in the audit brief: booking
financial math, no-show logic, timezone handling, and slot-overlap detection.
Build green; 518/518 unit tests pass. Integration tests need a running Docker
daemon and were not exercised here (Testcontainers could not reach
`npipe://./pipe/docker_engine`).

## Scope

- `server/src/ScholarPath.Application/ConsultantBookings/Commands/{RequestBooking,AcceptBooking,CancelBooking,MarkNoShow}/...`
- `server/src/ScholarPath.Application/ConsultantBookings/Services/RefundCalculatorService.cs`
- `server/src/ScholarPath.Application/ProfitShare/ProfitShareCalculator.cs`,
  `ProfitShareConfigResolver.cs`
- `server/src/ScholarPath.Application/FinancialConfig/{FinancialCalculator,FinancialRuleResolver}.cs`
- `server/src/ScholarPath.Application/Payments/Commands/{ProcessStripeWebhook,RefundPayment}/...`
- `server/src/ScholarPath.Application/ConsultantBookings/Queries/GetConsultantAvailability/...`
- `server/src/ScholarPath.Infrastructure/Services/ConsultantReadService.cs`
- `server/src/ScholarPath.Infrastructure/Jobs/{CompletionJob,MeetingNoShowSweepJob,StripePayoutJob}.cs`
- `server/src/ScholarPath.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`
  (`ConsultantBookingConfiguration`)
- `server/src/ScholarPath.Domain/Entities/{Bookings,Payments}.cs`
- `docs/PAYMENTS.md`, `docs/SRS.md` (FR-085..FR-091 refund matrix)

Note: there is no `CompleteBooking` command — auto-completion lives in
`CompletionJob` (Confirmed → Completed at 6h post-end when no no-show). The
audit brief's `CompleteBooking/` and `ProfitShareCalculator.cs` path under
`Common/Services/` are stale; the real locations are listed above.

## 1. Booking financial math — VERIFIED CORRECT

- **Gross in cents** at request time (`RequestBookingCommandHandler.cs:198`):
  `amountCents = (long)decimal.Round(priceUsd * 100m, 0, MidpointRounding.AwayFromZero)`
  — `decimal` arithmetic, banker-safe rounding, integer cents. Same conversion
  is used consistently in `AcceptBookingCommandHandler.cs:73-76` and the
  no-show / sweep paths.
- **Profit share = gross * percentage** is implemented in two layers:
  - `ProfitShareCalculator.Calculate` (legacy, FR-204 10% default).
  - `FinancialCalculator.Calculate` (current, FR-167/175 — supports a flat
    or percentage fee plus a profit-share percentage).
  Both use `Math.Round(gross * pct, MidpointRounding.AwayFromZero)` on cents,
  then `payee = gross - platformTake`, so the split always re-sums to gross
  exactly. `FinancialRuleResolver` caps the platform take at gross so a
  big fixed fee on a small gross can never push payee negative.
- **Capture-time split lock-in**: `AcceptBookingCommandHandler.cs:112-115`
  calls `FinancialRuleResolver.ResolvePaymentSplitAsync` and writes both
  `ProfitShareAmountCents` and `PayeeAmountCents` onto the `Payment` row.
  Payouts (`StripePayoutJob`) read `PayeeAmountCents` directly, so the split
  is a snapshot, not re-derived from a (mutable) config later.
- **Refund invariant `refunded + payeeNet <= gross`**: holds for the
  *post-capture* shape on the happy path (`refunded=0`, `profitShare + payee
  = gross`) and on a full refund (`refunded=gross`, no payout — see finding
  3.1 below for the partial-refund gap).
- **Unit-test coverage** is strong: `ProfitShareCalculatorTests`
  (8 cases, including resum-to-gross + rounding), `FinancialCalculatorTests`,
  `RefundCalculatorServiceTests` (11 cases including the 24h boundary,
  odd-cent halving, negative-amount guard, zero-amount, wrong-actor guards).

## 2. No-show logic — VERIFIED CORRECT (was a B1 blocker in the PB-006 review)

The PB-006 review (`docs/reviews/PB-006-tasneem-review.md`, B1) flagged that
`MarkNoShowCommandHandler` set the status but never issued the refund. The
current handler at `MarkNoShowCommandHandler.cs:80-124` does both:

- **Student marks consultant no-show** (`isStudent` branch, lines 80-124):
  - 6h window guard (after session end, before +6h) — lines 65-76.
  - Validates `StripePaymentIntentId` is present, computes
    `amountCents = priceUsd * 100m`, calls
    `_stripeService.RefundPaymentAsync` with the deterministic idempotency
    key `booking-noshow-refund:{booking.Id:N}`.
  - On refund success, marks `Payment.Status = Refunded`,
    `RefundedAmountCents = AmountCents`, sets `RefundedAt` and `RefundReason`.
  - Sets `IsNoShowConsultant = true`, `Status = NoShowConsultant`,
    `CancellationReason = ConsultantNoShow`.
- **Consultant marks student no-show** (`else` branch, lines 125-130):
  - No refund. Sets `IsNoShowStudent = true`, `Status = NoShowStudent`.
  - Payment stays `Captured` so `StripePayoutJob` pays the consultant the
    pre-locked `PayeeAmountCents` (gross minus platform's profit share),
    matching the SRS refund matrix (FR-091: student no-show -> 0% refund).

The same matrix is mirrored — and made robust against a forgotten manual
no-show mark — by `MeetingNoShowSweepJob` (FR-217 / `MeetingNoShowSweepJob.cs`),
which runs every 15 minutes and uses `StudentJoinedAt` / `ConsultantJoinedAt`
attendance flags to attribute the no-show. Both code paths share the
idempotency key shape (`booking-noshow-refund:{booking.Id:N}`) so a manual
and an automated mark cannot double-refund.

## 3. Refund flow — one product-decision gap flagged below

### 3.1. PartiallyRefunded consultant earnings are stranded (FLAGGED, not fixed)

`CancelBookingCommandHandler.cs:133-138` updates only
`RefundedAmountCents` / `Status` after a partial refund — it does **not**
adjust `PayeeAmountCents`. `StripePayoutJob.cs:30-35` then filters
`p.Status == PaymentStatus.Captured`, which excludes every
`PartiallyRefunded` payment from payout entirely.

Worked example (50% student cancel < 24h before, gross $100, 10% share):

| Field                     | After capture | After 50% refund |
|---------------------------|---------------|------------------|
| `AmountCents`             | 10000         | 10000            |
| `ProfitShareAmountCents`  | 1000          | 1000             |
| `PayeeAmountCents`        | 9000          | 9000 (unchanged) |
| `RefundedAmountCents`     | 0             | 5000             |
| `Status`                  | Captured      | PartiallyRefunded|

Two consequences:

1. The invariant `refunded + payeeNet <= gross` fails: `5000 + 9000 > 10000`.
2. The payout job's `Status == Captured` filter drops this row, so the
   consultant receives **zero** for a session the student paid 50% to attend.

This needs a product decision (does the consultant get the kept half minus
fees, or nothing?) before the right fix is obvious — I have **not** fixed it
here. The cleanest two-line repair that preserves the invariant *and* still
pays the consultant the proportional kept-portion would be:

```csharp
// In CancelBookingCommandHandler, after the partial-refund block:
payment.PayeeAmountCents = Math.Max(
    0,
    payment.AmountCents - payment.RefundedAmountCents - payment.ProfitShareAmountCents);
```

…plus widening `StripePayoutJob`'s filter to
`p.Status == PaymentStatus.Captured || p.Status == PaymentStatus.PartiallyRefunded`.
The admin `RefundPaymentCommandHandler.cs:131-138` has the same shape and
would need the same change. Recommend tracking as a follow-up task with a
spec note in `docs/PAYMENTS.md`.

### 3.2. Refund analytics also exclude PartiallyRefunded (FLAGGED)

`GetProfitShareAnalyticsQuery.cs:37` filters
`p.Status == PaymentStatus.Captured`, so PartiallyRefunded payments are
invisible to admin reporting. Self-consistent with the payout job today;
once 3.1 is resolved this filter should widen too.

## 4. Timezone handling — VERIFIED CORRECT

- **All persisted instants are UTC `DateTimeOffset`** — entities `ConsultantBooking`
  and `Payment` declare every timestamp as `DateTimeOffset` /
  `DateTimeOffset?`, and `RequestBookingCommandHandler.cs:52-53` explicitly
  converts incoming `ScheduledStartAt` / `ScheduledEndAt` to UTC with
  `.ToUniversalTime()` before saving.
- **Recurring-rule expansion is timezone-aware**:
  `ConsultantReadService.ExpandRecurringRule` (lines 269-337) iterates dates
  in the consultant's stored IANA/Windows timezone, skips DST spring-forward
  gap times via `tz.IsInvalidTime`, and converts to UTC via
  `TimeZoneInfo.ConvertTimeToUtc` before yielding each slot. The booking
  command applies the same conversion in reverse to validate against the
  recurring window (`RequestBookingCommandHandler.cs:147-163`), so a
  manipulated client can't book outside published availability.
- **Returned to the client with timezone metadata**: `BookableSlotDto`
  carries `StartAt`/`EndAt` as `DateTimeOffset` plus the explicit
  `Timezone` string of the rule (`BookingDtos.cs:136-147`), which is enough
  for the client to render in the viewer's locale and label the source zone.
- **Resolver fallback**: `TimeZoneResolver.Resolve` returns `TimeZoneInfo.Utc`
  for an unknown id rather than throwing — defensible default, but a
  consultant who configures a typo'd timezone silently degrades to UTC.
  Comment-only; not a bug for this audit.

## 5. Slot-overlap detection — VERIFIED CORRECT (with documented race)

Two layers prevent a consultant from holding two confirmed bookings that
overlap:

1. **Application-level interval overlap check** in
   `RequestBookingCommandHandler.cs:173-195`:
   ```csharp
   scheduledStartAtUtc < b.ScheduledEndAt && scheduledEndAtUtc > b.ScheduledStartAt
   ```
   Standard half-open overlap predicate, exercised against both the
   consultant and the requesting student so a student also can't double-book.
   Filtered to blocking statuses (`Requested`, `Confirmed`).
2. **DB-level unique filtered index** `UX_Bookings_Consultant_Slot_Active`
   on `(ConsultantId, ScheduledStartAt)` filtered on
   `[Status] IN ('Requested', 'Confirmed')`
   (`EntityConfigurations.cs:442-445`, migration
   `20260517015118_AddBookingSlotUniqueIndex_PB006.cs`). Two concurrent
   inserts for the same start time race-lose at the DB layer; the second
   raises a `ConflictException` (409) via
   `ApplicationDbContext.SaveChangesAsync`.

**Known limitation** (already flagged as N4 in the PB-006 review): the
filtered unique index only catches *same-start* races. Two concurrent
requests with overlapping-but-not-identical start times (e.g. 14:00-15:00
vs 14:30-15:30) can both pass the application check before either inserts.
SQL Server doesn't natively support an exclusion constraint over a range
predicate; the practical mitigations are an isolation-level Serializable
transaction around the check+insert or the existing optimistic concurrency
token. The 7-day Stripe authorization expiry plus `SessionExpiryJob` covers
the residual window. Not changed in this audit.

## 6. Other observations (not bugs, no action taken)

- `MarkNoShowCommandHandler.cs:87-90` computes the refund amount from
  `booking.PriceUsd * 100m` instead of `payment.AmountCents` — divergent
  from `CancelBookingCommandHandler.cs:78-85` which uses
  `payment.AmountCents`. `PriceUsd` is not mutated after request creation,
  so the two are equal today; the cancel handler's `payment.AmountCents`
  source is the authoritative one and should be preferred. PB-006 review N2
  already tracks a `MoneyExtensions.ToCents()` cleanup.
- `AcceptBookingCommandHandler.cs:103` applies the split only when
  `Payment.Status` is `Held` or `Pending`. If the
  `payment_intent.succeeded` webhook from `ProcessStripeWebhookCommand`
  flips the row to `Captured` *before* `AcceptBookingCommand` reloads it,
  the split never runs and the row keeps the request-time defaults
  (`ProfitShare = 0`, `Payee = full`). The accept handler runs the Stripe
  capture call itself, which is what triggers that webhook — so in the
  canonical flow the in-memory entity is still `Held` when the condition
  fires. Worth a code comment but not a fixable bug without rearchitecting
  the capture/webhook order.

## How to reproduce the test run

```
cd server
dotnet restore ScholarPath.slnx
dotnet build ScholarPath.slnx -c Release --no-restore
dotnet test ScholarPath.slnx --filter "Category!=Integration" -c Release --no-build
# -> Passed: 518, Failed: 0, Skipped: 0 on ScholarPath.UnitTests.dll
# Integration tests need a running Docker daemon (Testcontainers).
```

## Verdict

No new bugs introduced in this audit. The B1 from the PB-006 review
(`MarkNoShow` refund execution) is verified fixed. One pre-existing
product-decision gap is documented for follow-up: section 3.1
(PartiallyRefunded payments are excluded from payout). Build is green and
unit tests pass.
