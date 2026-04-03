# PB-006 — Consultant Booking, Payment, Rating

**Owner**: @TasneemShaaban • **Priority**: Essential • **Iteration**: 3 • **Est**: 74 pts

## Problem statement

Students book 1:1 sessions with verified Consultants. Consultants set availability slots + fee. Bookings work on a hold→capture Stripe model: student card held on request, captured only on Consultant acceptance; released on reject/expire. Cancellation + no-show refund rules per SRS (FR-085..FR-091). Post-session ratings drive Consultant reputation; low-rated profiles auto-suspended.

## User stories

| ID | Story | Size |
|----|-------|------|
| US-051 | Browse Consultant profiles | 3pt |
| US-052 | View expertise/credentials/availability/ratings/fee | 4pt |
| US-053 | Consultant manages availability slots | 4pt |
| US-054 | Student requests a booking | 4pt |
| US-055 | Stripe payment hold on booking request | 5pt |
| US-056 | Consultant accepts/rejects booking | 4pt |
| US-057 | Capture payment on acceptance | 5pt |
| US-058 | Release hold on reject/expire | 4pt |
| US-059 | Full refund if student cancels before acceptance | 3pt |
| US-060 | Full refund if student cancels >24h before accepted session | 3pt |
| US-061 | 50% refund if student cancels <24h after acceptance | 3pt |
| US-062 | Full refund if Consultant cancels after acceptance | 3pt |
| US-063 | Full refund if Consultant marked no-show | 3pt |
| US-064 | No refund if Student no-show | 3pt |
| US-065 | Student views booking + refund status | 3pt |
| US-066 | Consultant views bookings + earnings | 3pt |
| US-067 | Post-session rating by Student | 3pt |
| US-068 | Consultant views ratings | 2pt |
| US-069 | Admin moderates ratings | 3pt |
| US-070 | Auto-suspend low-rated Consultants | 4pt |
| US-071 | Stripe webhook log for idempotency | 5pt |

## Functional requirements

FR-076 .. FR-101

## Acceptance criteria

1. **Availability** — Consultant defines weekly recurring slots + ad-hoc date ranges. Timezone-aware (store UTC, render in user TZ).
2. **Booking request** — Student picks slot + duration → Stripe PaymentIntent in `requires_capture` state (funds held). Booking row created with status `Requested`.
3. **24h expiry** — If Consultant doesn't respond in 24h → auto-expire: PaymentIntent canceled (funds released), booking status `Expired`, Student notified.
4. **Accept** — Consultant accepts → capture PaymentIntent → booking status `Confirmed` → meeting link generated (external integration TBD — e.g., daily.co or Jitsi).
5. **Reject** — Consultant rejects → cancel PaymentIntent (release funds) → status `Rejected`.
6. **Cancellation refund matrix** — Enforced server-side per SRS Section 5:
   | Event | Refund |
   |-------|--------|
   | Student cancels before Consultant acceptance | 100% |
   | Student cancels >24h before accepted session | 100% |
   | Student cancels <24h before accepted session | 50% |
   | Consultant cancels after acceptance | 100% |
   | Consultant no-show | 100% |
   | Student no-show | 0% |
7. **No-show marking** — Either party can mark the other as no-show within 6h of session end; admin reviews disputes.
8. **Rating** — Post-session (status `Completed`), Student rates 1–5 + comment. One per booking.
9. **Auto-suspend** — Consultant's moving-average rating over last 20 sessions < 3.0 → `AccountStatus=Suspended` pending Admin review.
10. **Idempotency** — Every Stripe webhook verified by signature + stored in `StripeWebhookEvent` with idempotency key to prevent replay.

## Non-goals

- Group sessions (1:1 only in v1)
- Recurring booking packages (v2)
- Integrated video call UI (use external link only)
