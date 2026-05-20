# PB-006 — Tasks

**Owner**: @TasneemShaaban  •  **Est**: 74 pts  •  **Iteration**: 3
**Status**: ✅ backend + frontend + unit + integration tests shipped; E2E pending staging.

## Backend
- [x] T-001 — `ConsultantAvailability` + `ConsultantBooking` entities + configs (timezone-aware)  *(`Domain/Entities/ConsultantBooking.cs` + EF configs; UTC stored, IANA timezone in row)*
- [x] T-002 — `UpdateAvailabilityCommand` + slot generation (weekly recurring + ad-hoc)  *(`Application/ConsultantBookings/Commands/UpdateAvailability/`)*
- [x] T-003 — `RequestBookingCommand` → creates PaymentIntent hold + booking row (FR-078, FR-186)  *(`RequestBooking/` — Stripe PaymentIntent created, status Requested)*
- [x] T-004 — `AcceptBookingCommand` → capture PaymentIntent + meeting link (FR-081, FR-187)  *(`AcceptBooking/` — PaymentIntent captured; ACS meeting room created)*
- [x] T-005 — `RejectBookingCommand` → cancel PaymentIntent (FR-082, FR-188)  *(`RejectBooking/` — PaymentIntent cancelled)*
- [x] T-006 — `SessionExpiryJob` (Hangfire) — auto-expire after 24h  *(`Infrastructure/Jobs/SessionExpiryJob.cs`)*
- [x] T-007 — `CancelBookingCommand` + `RefundCalculatorService` (FR-085..FR-091)  *(`CancelBooking/` + `Services/RefundCalculatorService.cs`)*
- [x] T-008 — `MarkNoShowCommand` (either party; 6h window)  *(`MarkNoShow/`)*
- [x] T-009 — `CompletionJob` — auto-complete 6h after scheduled end if no no-show  *(`Infrastructure/Jobs/CompletionJob.cs`)*
- [x] T-010 — `SubmitConsultantRatingCommand` + auto-suspend check (FR-094)  *(`SubmitConsultantRating/`)*
- [x] T-011 — Stripe webhook idempotency + all events handled (FR-195)  *(`API/Controllers/WebhooksController.cs` + `Payments/Commands/ProcessStripeWebhook/`)*
- [x] T-012 — Unit tests for `RefundCalculatorService` matrix (all 6 combos)  *(`tests/ScholarPath.UnitTests/ConsultantBookings/Services/RefundCalculatorServiceTests.cs`)*
- [x] T-013 — Integration tests: request→accept→complete→rate; request→expire; cancel at each stage  *(`tests/ScholarPath.IntegrationTests/ConsultantBookings/` — 5 test files)*

## Frontend
- [x] T-014 — `ConsultantsBrowse.tsx` directory with filters  *(`pages/student/ConsultantsBrowse.tsx` — specialization filter, rating sort, pagination)*
- [x] T-015 — `ConsultantDetail.tsx` — profile + availability + book CTA  *(`pages/student/ConsultantDetail.tsx`)*
- [x] T-016 — `BookingCheckout.tsx` — Stripe Elements flow  *(`pages/student/BookingCheckout.tsx` — clientSecret guard, notes field)*
- [x] T-017 — Consultant `AvailabilityEditor.tsx` — weekly slots UI  *(`pages/consultant/ConsultantAvailability.tsx`)*
- [x] T-018 — Consultant `IncomingBookings.tsx` — accept/reject list  *(`pages/consultant/ConsultantBookings.tsx` — accept/reject/noshow actions)*
- [x] T-019 — `MyBookings.tsx` (role-aware)  *(`pages/student/StudentBookings.tsx` + `pages/student/StudentBookingDetails.tsx`)*
- [x] T-020 — `RatingModal.tsx` post-session  *(`components/company/RatingModal.tsx` — triggered on booking completion)*
- [x] T-021 — Real-time updates via SignalR  *(`hooks/useNotificationHub.ts` — invalidates unread count + toasts on booking events from PB-018 streaming handlers)*
- [x] T-022 — Arabic copy review  *(`locales/ar/bookings.json` + `locales/ar/consultants.json` + `locales/ar/consultantPortal.json` — full AR)*

## QA
- [x] T-023 — E2E happy path: book → accept → complete → rate  *(`client/src/test/e2e/booking.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` + `E2E_CONSULTANT_EMAIL` set)*
- [x] T-024 — E2E cancellation scenarios (before accept, >24h, <24h)  *(`client/src/test/e2e/booking.spec.ts` — full flow skips unless credentials set)*

## Done criteria
- [x] Refund matrix test-verified; webhook replay tests pass; CI green.
- [ ] E2E green in staging. *(spec written — `booking.spec.ts`; needs staging credentials to run)*  *(spec: `client/src/test/e2e/booking.spec.ts`; run `npm run test:e2e:local` or `.github/workflows/e2e.yml`; see `docs/E2E-TESTING.md`)*
