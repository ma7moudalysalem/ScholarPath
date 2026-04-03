# PB-006 — Tasks

**Owner**: @TasneemShaaban  •  **Est**: 74 pts  •  **Iteration**: 3

## Backend
- [ ] T-001 — `ConsultantAvailability` + `ConsultantBooking` entities + configs (timezone-aware)
- [ ] T-002 — `UpdateAvailabilityCommand` + slot generation (weekly recurring + ad-hoc)
- [ ] T-003 — `RequestBookingCommand` → creates PaymentIntent hold + booking row (FR-078, FR-186)
- [ ] T-004 — `AcceptBookingCommand` → capture PaymentIntent + meeting link (FR-081, FR-187)
- [ ] T-005 — `RejectBookingCommand` → cancel PaymentIntent (FR-082, FR-188)
- [ ] T-006 — `SessionExpiryJob` (Hangfire) — auto-expire after 24h
- [ ] T-007 — `CancelBookingCommand` + `RefundCalculatorService` (FR-085..FR-091)
- [ ] T-008 — `MarkNoShowCommand` (either party; 6h window)
- [ ] T-009 — `CompletionJob` — auto-complete 6h after scheduled end if no no-show
- [ ] T-010 — `SubmitConsultantRatingCommand` + auto-suspend check (FR-094)
- [ ] T-011 — Stripe webhook idempotency + all events handled (FR-195)
- [ ] T-012 — Unit tests for `RefundCalculatorService` matrix (all 6 combos)
- [ ] T-013 — Integration tests: request→accept→complete→rate; request→expire; cancel at each stage

## Frontend
- [ ] T-014 — `ConsultantsBrowse.tsx` directory with filters
- [ ] T-015 — `ConsultantDetail.tsx` — profile + availability + book CTA
- [ ] T-016 — `BookingCheckout.tsx` — Stripe Elements flow
- [ ] T-017 — Consultant `AvailabilityEditor.tsx` — weekly slots UI
- [ ] T-018 — Consultant `IncomingBookings.tsx` — accept/reject list
- [ ] T-019 — `MyBookings.tsx` (role-aware)
- [ ] T-020 — `RatingModal.tsx` post-session
- [ ] T-021 — Real-time updates via SignalR
- [ ] T-022 — Arabic copy review

## QA
- [ ] T-023 — E2E happy path: book → accept → complete → rate
- [ ] T-024 — E2E cancellation scenarios (before accept, >24h, <24h)

## Done criteria
Refund matrix test-verified; webhook replay tests pass; CI green.
