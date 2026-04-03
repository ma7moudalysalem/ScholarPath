# PB-005 — Tasks

**Owner**: @yousra-elnoby  •  **Est**: 23 pts  •  **Iteration**: 3

## Backend
- [ ] T-001 — `CompanyReviewPayment` entity + config
- [ ] T-002 — `SubmitCompanyRatingCommand` + validator (one per application; post-decision)
- [ ] T-003 — `CaptureCompanyReviewPaymentCommand` triggered on `ApplicationStatusChangedEvent → Accepted|Rejected`
- [ ] T-004 — `RefundCompanyReviewCommand` for all refund cases (withdrawn pre/post, auto-timeout)
- [ ] T-005 — `CompanyReviewTimeoutRefundJob` (Hangfire, runs daily)
- [ ] T-006 — Stripe webhook branch for Company payment events
- [ ] T-007 — `GetCompanyRatingsQuery` with aggregation (avg + count)
- [ ] T-008 — Unit + integration tests

## Frontend
- [ ] T-009 — Rating modal shown post-decision (triggered on kanban status change)
- [ ] T-010 — Company dashboard page — received ratings grid
- [ ] T-011 — Fee display in application submit confirmation screen
- [ ] T-012 — Arabic copy review

## Done criteria
Refund matrix green in unit tests; webhook idempotency verified.
