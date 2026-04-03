# PB-005 — Implementation Plan

## Architecture touchpoints

### Domain
- `CompanyReview` (rating + comment tied to `ApplicationTrackerId`)
- `CompanyReviewPayment` (Stripe PaymentIntent ID, amount, status, refund info, idempotency key)
- Reuses `Payment` entity from PB-013

### Application (`server/src/ScholarPath.Application/CompanyReviews/`)
- **Commands**: `SubmitCompanyRatingCommand`, `RefundCompanyReviewCommand` (system-triggered), `CaptureCompanyReviewPaymentCommand` (system)
- **Queries**: `GetCompanyRatingsQuery`, `GetReviewPaymentQuery`
- **Services**: `CompanyReviewPricingService` (reads from `ProfitShareConfig` from PB-014)

### Infrastructure
- Stripe webhook handler: on `payment_intent.succeeded` → persist; on `charge.refunded` → update refund status
- Hangfire job `CompanyReviewTimeoutRefundJob` — runs daily, auto-refunds applications where Company hasn't reviewed within 14d of deadline

### API
- `POST /api/company-reviews` (submit rating)
- `GET  /api/companies/{id}/reviews`
- Webhook-driven; no public payment endpoints beyond PB-013

### Frontend
- Student: Rating modal post-decision
- Company: Received-ratings page in dashboard

### Tests
- Unit: refund calculation matrix (withdrawn pre/post, auto-timeout, rejected)
- Integration: full pay → review → capture flow
- E2E: rate a Company after decision

## Dependencies
PB-001, PB-004, PB-013, PB-014
