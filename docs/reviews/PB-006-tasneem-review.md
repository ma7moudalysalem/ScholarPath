# Code Review — `feat/PB-006-booking-entities-config`

**Reviewer**: @ma7moudalysalem · **Author**: @TasneemShaaban · **Date**: 2026-04-25
**Branch**: `feat/PB-006-booking-entities-config` · **vs**: `main` · **PR**: #20
**Scope**: PB-006 Consultant Booking — backend slice (T-001..T-013)
**Stats**: +6,403 / −31 across 50 files · 19 commits · all 13 backend tasks delivered

---

## TL;DR

Excellent work, Tasneem — this is the largest vertical slice landed in the project so far, and the structure is consistent with Clean Architecture. Test coverage on the refund matrix is strong. You've delivered **all 13 backend tasks** in the spec (frontend T-014..T-022 still pending).

That said, there are **3 blockers** that must be resolved before merge, plus ~10 architectural / polish notes. Each finding includes file:line and a suggested fix.

**Verdict**: ✅ approve once the blockers and the folder typo are addressed.

---

## 🔴 Blockers (must-fix before merge)

### B1. `MarkNoShowCommandHandler` doesn't actually issue the refund
[`server/src/ScholarPath.Application/ConsultantBookings/Commands/MarkNoShow/MarkNoShowCommandHandler.cs:79`](server/src/ScholarPath.Application/ConsultantBookings/Commands/MarkNoShow/MarkNoShowCommandHandler.cs:79)

The spec (FR-088 + acceptance criterion #6) requires: **Consultant no-show → 100% refund to the student**. The handler sets `Status = NoShowConsultant` and `CancellationReason = ConsultantNoShow`, but **never calls `_stripeService.RefundPaymentAsync`**. Result: the student paid, the consultant didn't show up, and no money is returned.

**Fix**:
```csharp
if (isStudent) // student is marking the consultant as no-show
{
    var idempotencyKey = $"booking-noshow-refund:{booking.Id:N}";
    await _stripeService.RefundPaymentAsync(
        paymentIntentId: booking.StripePaymentIntentId!,
        amountCents: (long)decimal.Round(booking.PriceUsd * 100m, 0, MidpointRounding.AwayFromZero),
        reason: CancellationReason.ConsultantNoShow.ToString(),
        idempotencyKey: idempotencyKey,
        ct: cancellationToken);

    booking.IsNoShowConsultant = true;
    booking.Status = BookingStatus.NoShowConsultant;
    booking.CancellationReason = CancellationReason.ConsultantNoShow;
}
// Student no-show case: no refund (spec FR-091) — current code is fine.
```

Add an integration test for this scenario.

---

### B2. Folder/namespace mismatch + typo: `UpdateAvailabilty`
[`server/src/ScholarPath.Application/ConsultantBookings/Commands/UpdateAvailabilty/`](server/src/ScholarPath.Application/ConsultantBookings/Commands/UpdateAvailabilty/)

- Folder name: `UpdateAvailabilty` (missing `i`)
- Namespace inside the files: `UpdateAvailability` (correct)
- The controller imports `ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability` (correct)

The build passes, but folder and namespace must match — that's a project convention. Rename the folder to `UpdateAvailability`:

```bash
git mv .../Commands/UpdateAvailabilty .../Commands/UpdateAvailability
```

---

### B3. `HandleStripeWebhook` lives under the wrong slice
[`server/src/ScholarPath.Application/ConsultantBookings/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs:11`](server/src/ScholarPath.Application/ConsultantBookings/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs:11)

- File location: `ConsultantBookings/Commands/HandleStripeWebhook/`
- Namespace in the code: `ScholarPath.Application.Payments.Commands.HandleStripeWebhook`

Stripe webhooks aren't booking-specific (PB-013 and PB-005 will route through the same webhook). Move the folder to `Payments/Commands/HandleStripeWebhook/` so the folder matches the namespace and the code lives next to the other epics that share it.

---

## 🟡 Important — architecture / convention

### I1. No `[Auditable]` attribute on any command
Every state-mutating command (Request/Accept/Reject/Cancel/MarkNoShow/SubmitConsultantRating/UpdateAvailability) needs the attribute. The `AuditBehavior` introduced in PB-012 picks it up automatically — zero boilerplate. This is one of the constitution principles.

```csharp
[Auditable(AuditAction.BookingAccepted, "ConsultantBooking")]
public sealed class AcceptBookingCommand : IRequest { ... }
```

### I2. `IConfiguration` is being injected in the Application layer
[`HandleStripeWebhookCommandHandler.cs:16`](server/src/ScholarPath.Application/ConsultantBookings/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs:16)

The Application layer shouldn't know about `Microsoft.Extensions.Configuration` — that's a cross-layer leak. Use `IOptions<StripeSettings>` with a `WebhookSecret` property instead:

```csharp
public class StripeSettings { public string WebhookSecret { get; set; } = ""; }

// In Infrastructure DI:
services.Configure<StripeSettings>(config.GetSection("Stripe"));

// In the handler:
private readonly IOptions<StripeSettings> _stripeOptions;
// usage: _stripeOptions.Value.WebhookSecret
```

### I3. Missing Stripe event types
[`HandleStripeWebhookCommandHandler.cs:87-105`](server/src/ScholarPath.Application/ConsultantBookings/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs:87)

The switch handles 4 event types. The important ones still missing:
- `payment_intent.payment_failed` — Status=Failed + auto-cancel the booking
- `charge.dispute.created` — Status=Disputed + admin notification
- `payment_intent.requires_action` — for SCA / 3D Secure flows

Adding the `payment_failed` case now is enough; the rest can be tracked as TODOs in `_Module.md`.

### I4. Booking status isn't updated from the webhook
When `payment_intent.succeeded` arrives for a manual-capture PI, you update `Payment.Status` but never touch `Booking.Status`. This works today because Stripe's capture call returns synchronously inside `AcceptBookingCommand`, but if the webhook lags, `Booking.Status = Confirmed` and `Payment.Status = Held` will diverge for a few seconds.

Add a defensive update:
```csharp
// In HandlePaymentIntentSucceededAsync, after updating the Payment:
var booking = await _context.Bookings.FirstOrDefaultAsync(
    b => b.StripePaymentIntentId == paymentIntentId, cancellationToken);
if (booking is { Status: BookingStatus.Requested })
{
    booking.Status = BookingStatus.Confirmed;
    booking.ConfirmedAt = DateTimeOffset.UtcNow;
}
```

### I5. Exception types
Every business-rule violation throws `InvalidOperationException`. The exception-handler middleware then returns 500. Introduce a domain exception:

```csharp
// Domain/Exceptions/BookingDomainException.cs
public class BookingDomainException : Exception { ... }

// Usage:
throw new BookingDomainException("Only requested bookings can be accepted.");
```

The middleware then maps it to 422 Unprocessable Entity with RFC 7807 problem details. The pattern already exists in `Middleware/ExceptionHandlerMiddleware.cs` — model after it.

### I6. No domain events
When a booking transitions to Confirmed or Completed, **no event** is published. PB-010 (Notifications) and PB-018 (Real-time/anomaly detection) both depend on these events. Add them:

```csharp
// In AcceptBookingCommandHandler, after SaveChanges:
await _mediator.Publish(new BookingConfirmedEvent(booking.Id), cancellationToken);
```

Define `BookingConfirmedEvent`, `BookingCompletedEvent`, `BookingCancelledEvent` in `Domain/Events/`. MediatR will dispatch any registered handlers automatically.

---

## 🟢 Nice-to-have / polish

### N1. `RefundCalculatorService.Calculate(...)` takes 7 parameters
[`RefundCalculatorService.cs:13`](server/src/ScholarPath.Application/ConsultantBookings/Services/RefundCalculatorService.cs:13)

The signature is long and easy to mis-order. Use an input record:

```csharp
public sealed record RefundCalculationContext(
    BookingStatus BookingStatus,
    Guid CancelledByUserId,
    Guid StudentId,
    Guid ConsultantId,
    DateTimeOffset ScheduledStartAt,
    decimal PriceUsd,
    DateTimeOffset NowUtc);

public RefundCalculationResult Calculate(RefundCalculationContext ctx) { ... }
```

### N2. Repeated rounding logic
`amountCents = (long)decimal.Round(priceUsd * 100m, 0, MidpointRounding.AwayFromZero)` is duplicated in:
- `RequestBookingCommandHandler:147`
- `AcceptBookingCommandHandler:64`
- `RefundCalculatorService:21`

Extract an extension method:
```csharp
public static class MoneyExtensions
{
    public static long ToCents(this decimal usd) =>
        (long)decimal.Round(usd * 100m, 0, MidpointRounding.AwayFromZero);
}
```

### N3. `RefundCalculator` tests are missing edge cases
[`RefundCalculatorServiceTests.cs`](server/tests/ScholarPath.UnitTests/ConsultantBookings/Services/RefundCalculatorServiceTests.cs)

The 8 cases are great, but missing:
- **Exact 24h boundary** — `scheduledStartAt = nowUtc.AddHours(24)` (currently falls into the 50% bucket because of `>`).
- **Rounding edge** — what does `priceUsd = 99.99m` produce?
- **Zero price** — if fee = 0, `Calculate` doesn't throw. Intentional?

### N4. `RequestBookingCommandHandler` — race on the same slot
[`RequestBookingCommandHandler.cs:122`](server/src/ScholarPath.Application/ConsultantBookings/Commands/RequestBooking/RequestBookingCommandHandler.cs:122)

If two students submit on the same slot concurrently, the `consultantHasConflict` check can pass for both before either inserts. A unique-overlap constraint is hard in SQL Server, so the practical fixes are:
- Use `IsolationLevel.Serializable` on the transaction
- Add an optimistic concurrency token on `Bookings.Status`

There's a low-probability window where the second student gets a PaymentIntent and then the insert throws — Stripe auto-releases after 7 days if not captured, plus `SessionExpiryJob` covers the short term.

### N5. `RequestBookingCommandHandler` — recurring slot validation missing
[`RequestBookingCommandHandler.cs:104`](server/src/ScholarPath.Application/ConsultantBookings/Commands/RequestBooking/RequestBookingCommandHandler.cs:104)

There's validation for ad-hoc availability slots only. Recurring (weekly) slots are not validated — a student could book outside the recurring window. Add:

```csharp
if (availability.IsRecurring && availability.DayOfWeek.HasValue)
{
    if (scheduledStartAtUtc.DayOfWeek != availability.DayOfWeek.Value)
        throw new InvalidOperationException("Booking day does not match recurring slot.");
    // also check time-of-day window: availability.StartTimeOfDay / EndTimeOfDay
}
```

### N6. Two `SaveChangesAsync` in `RequestBookingCommandHandler`
[`RequestBookingCommandHandler.cs:241+244`](server/src/ScholarPath.Application/ConsultantBookings/Commands/RequestBooking/RequestBookingCommandHandler.cs:241)

Today you insert the Booking and Payment first (with `payment.RelatedBookingId = null`), then update `Payment.RelatedBookingId`. That's 2 round-trips and not transactional. Two options:
- Set `Booking.Payment = payment` and let EF wire the FK after generating `Booking.Id` in the same SaveChanges. EF supports this when the inverse navigation is configured.
- Wrap the code in an `IDbContextTransaction`.

### N7. Auto-suspend is silent — no log
[`SubmitConsultantRatingCommandHandler.cs:96`](server/src/ScholarPath.Application/ConsultantBookings/Commands/SubmitConsultantRating/SubmitConsultantRatingCommandHandler.cs:96)

When avg < 3.0 you suspend the consultant without logging anything. Add:
```csharp
_logger.LogWarning("Auto-suspended consultant {ConsultantId} (avg rating {Avg} over last 20 sessions)",
    consultant.Id, average);
```
And mark the command as [Auditable] (see I1).

### N8. `_logger` not injected in several handlers
The Stripe interactions in `AcceptBookingCommandHandler` and `CancelBookingCommandHandler` log nothing. If a capture fails, you get a 500 in middleware with no context. Inject `ILogger<T>` and log before/after each Stripe call.

### N9. `SessionExpiryJob` doesn't notify the student
[`SessionExpiryJob.cs:60`](server/src/ScholarPath.Infrastructure/Jobs/SessionExpiryJob.cs:60)

Acceptance criterion #3 says "Student notified" on expire. Add a notification dispatch after `SaveChangesAsync`. Madiha's PB-010 has `INotificationService` you can call.

### N10. `BookingsController` route inconsistency
[`BookingsController.cs:24`](server/src/ScholarPath.API/Controllers/BookingsController.cs:24)

```csharp
[Route("api/[controller]")]   // → api/bookings
...
[HttpPost("/api/consultants/{id:guid}/book")]   // absolute path — bypasses the prefix
[HttpPatch("me/availability")]                   // → api/bookings/me/availability — confusing
```

Suggestion: rename the controller to `ConsultantBookingsController` and move the availability action to a `ConsultantsController` if one exists. Mixing absolute routes (starting with `/`) with the controller prefix is confusing.

### N11. Indentation in `SubmitConsultantRating` action
[`BookingsController.cs:106-110`](server/src/ScholarPath.API/Controllers/BookingsController.cs:106)

```csharp
public async Task<IActionResult> SubmitConsultantRating(
Guid id,                              // ← misaligned (1 indent level deep)
[FromBody] SubmitConsultantRatingCommand command,
CancellationToken cancellationToken)
```

`dotnet format` will fix this.

---

## ⚪ Spec gaps — missing tasks

| Task | Status |
|---|---|
| T-014..T-022 Frontend (ConsultantsBrowse, ConsultantDetail, BookingCheckout, AvailabilityEditor, IncomingBookings, MyBookings, RatingModal, SignalR, AR copy) | ❌ not done |
| T-023..T-024 E2E tests | ❌ |
| T-009 CompletionJob notification dispatch (see N9) | partial |
| T-011 Webhook events missing (see I3) | partial |
| Auto-suspend admin notification | missing |

The frontend slice can land in a follow-up PR. If helpful, look at PB-008/PB-011 for the patterns:
- `client/src/pages/admin/AiEconomyPage.tsx` — example of page structure
- `client/src/components/ai/RecommendationCard.tsx` — example of component structure
- EN+AR copy in `client/src/locales/{en,ar}/`

---

## ✅ Things I enjoyed reviewing

- **`RefundCalculatorService` is clean and standalone** — exact pattern I was hoping for. Tests are AAA-structured.
- **Idempotency keys on every Stripe call** — `booking-request:{...}`, `booking-accept:{...}`, `booking-refund:{...}:{percent}`. Excellent.
- **Conflict detection** on both the student and consultant sides — overlapping-interval logic is correct.
- **Webhook handler persists every event even for unknown types** — gives us a complete audit trail.
- **`MarkNoShowCommandHandler` time-window enforcement** — `nowUtc < sessionEndUtc` and `nowUtc > sessionEndUtc.AddHours(6)` is correct.
- **`SessionExpiryJob` per-booking try/catch** — one bad booking won't break the batch.
- **`SubmitConsultantRating` duplicate prevention via `alreadyRated` check** — and the auto-suspend logic is sound.
- **`RefineConsultantBookingSchema` migration** — schema changes are consistent, only 87 lines in the .cs (the Designer.cs is auto-generated).

---

## Pre-merge checklist

- [ ] B1 — refund execution in `MarkNoShowCommandHandler`
- [ ] B2 — rename `UpdateAvailabilty` → `UpdateAvailability`
- [ ] B3 — move `HandleStripeWebhook` to `Payments/Commands/`
- [ ] I1 — `[Auditable]` on the 7 commands
- [ ] I2 — `IOptions<StripeSettings>` instead of `IConfiguration`
- [ ] N3 — 24h boundary test
- [ ] Rebase onto `main` (the branch is 20 commits behind because of the analytics work)
- [ ] CI green on all 3 lanes (backend + client + security)

I1, I3, I4, I5, I6 can be deferred to a follow-up PR if time is tight — B1/B2/B3 are the blockers.

Great work, Tasneem 👏 — biggest slice yet and the structure is consistent. Knock out the blockers and we'll merge.
