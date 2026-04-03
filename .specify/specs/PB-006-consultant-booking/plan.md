# PB-006 — Implementation Plan

## Architecture touchpoints

### Domain
- `ConsultantAvailability`, `ConsultantBooking`, `ConsultantReview`
- Enums: `BookingStatus { Requested, Confirmed, Rejected, Expired, Cancelled, Completed, NoShowStudent, NoShowConsultant }`, `CancellationReason`
- Events: `BookingRequestedEvent`, `BookingConfirmedEvent`, `BookingCancelledEvent`, `BookingCompletedEvent`

### Application (`server/src/ScholarPath.Application/ConsultantBookings/`)
- **Commands**: `RequestBookingCommand`, `AcceptBookingCommand`, `RejectBookingCommand`, `CancelBookingCommand`, `MarkNoShowCommand`, `CompleteBookingCommand`, `SubmitConsultantRatingCommand`, `UpdateAvailabilityCommand`
- **Queries**: `GetConsultantDirectoryQuery` (filters), `GetConsultantPublicProfileQuery`, `GetMyBookingsQuery` (Student + Consultant views), `GetAvailableSlotsQuery`
- **Services**: `RefundCalculatorService` (pure function implementing the matrix), `SessionExpiryJob` (Hangfire — 24h auto-expire), `CompletionJob` (Hangfire — auto-complete 6h after session end)

### Infrastructure
- `StripeService` methods: `CreatePaymentIntentWithCapture`, `CapturePaymentIntent`, `CancelPaymentIntent`, `RefundPaymentIntent`
- Webhook handlers: `payment_intent.amount_capturable_updated`, `payment_intent.succeeded`, `payment_intent.canceled`, `charge.refunded`

### API (`server/src/ScholarPath.API/Controllers/BookingsController.cs`)
- `GET  /api/consultants/directory`
- `GET  /api/consultants/{id}`
- `GET  /api/consultants/{id}/available-slots`
- `POST /api/consultants/{id}/book`
- `POST /api/bookings/{id}/accept` (Consultant)
- `POST /api/bookings/{id}/reject` (Consultant)
- `POST /api/bookings/{id}/cancel` (either party)
- `POST /api/bookings/{id}/no-show` (either party)
- `POST /api/bookings/{id}/rating` (Student)
- `GET  /api/bookings/me` (role-aware)
- `PATCH /api/me/availability` (Consultant)

### Frontend
- Student: `ConsultantsBrowse`, `ConsultantDetail`, `BookingCheckout` (Stripe Elements), `MyBookings`, `RatingModal`
- Consultant: `AvailabilityEditor`, `IncomingBookings` (accept/reject UI), `MyBookings`, `Earnings`
- Real-time: `NotificationHub` pushes booking state changes

### Tests
- Unit: `RefundCalculatorService` matrix (all 6 combos)
- Integration: full request → accept → capture → complete → rating flow
- Integration: request → expire → funds released
- E2E: book → pay (Stripe test card) → accept → show booking in dashboard

## Dependencies
PB-001, PB-002, PB-010, PB-013, PB-014

## Risks
1. Timezone bugs — store UTC, test with users in multiple TZ
2. Stripe webhook ordering — use idempotency + state machine
3. Concurrent cancellations — optimistic concurrency via `RowVersion`
