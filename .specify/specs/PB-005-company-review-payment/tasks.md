# PB-005 — Tasks

**Owner**: @yousra-elnoby  •  **Est**: 23 pts  •  **Iteration**: 3
**Status**: ✅ backend + frontend shipped; E2E pending staging.

## Backend
- [x] T-001 — `CompanyReviewPayment` entity + config  *(`Domain/Entities/CompanyReviewPayment.cs` + EF config)*
- [x] T-002 — `SubmitCompanyRatingCommand` + validator (one per application; post-decision)  *(`Application/CompanyReviews/Commands/SubmitCompanyRating/`)*
- [x] T-003 — `CaptureCompanyReviewPaymentCommand` triggered on `ApplicationStatusChangedEvent → Accepted|Rejected`  *(`Application/CompanyReviews/Commands/CaptureCompanyReviewPayment/` + `EventHandlers/ApplicationStatusChangedEventHandler.cs`)*
- [x] T-004 — `RefundCompanyReviewCommand` for all refund cases (withdrawn pre/post, auto-timeout)  *(`Application/CompanyReviews/Commands/RefundCompanyReview/`)*
- [x] T-005 — `CompanyReviewTimeoutRefundJob` (Hangfire, runs daily)  *(`Infrastructure/Jobs/CompanyReviewTimeoutRefundJob.cs`)*
- [x] T-006 — Stripe webhook branch for Company payment events  *(`API/Controllers/WebhooksController.cs` + `Payments/Commands/ProcessStripeWebhook/` — company review payment events handled)*
- [x] T-007 — `GetCompanyRatingsQuery` with aggregation (avg + count)  *(`Application/CompanyReviews/Queries/GetCompanyRatings/`)*
- [x] T-008 — Unit + integration tests  *(`tests/ScholarPath.UnitTests/CompanyReviews/` — HideCompanyReviewCommandHandlerTests, RefundCompanyReviewCommandHandlerTests, SubmitCompanyRatingCommandHandlerTests)*

## Frontend
- [x] T-009 — Rating modal shown post-decision (triggered on kanban status change)  *(`components/company/RatingModal.tsx` — triggered on booking completion/application decision)*
- [x] T-010 — Company dashboard page — received ratings grid  *(`pages/company/Dashboard.tsx` — CompanyRatingsSummaryDto, average rating, recent reviews)*
- [x] T-011 — Fee display in application submit confirmation screen  *(`pages/student/ScholarshipDetail.tsx` + `pages/student/StudentBookingDetails.tsx` — review fee shown)*
- [x] T-012 — Arabic copy review  *(`locales/ar/` — company review / ratings strings present)*

## Done criteria
Refund matrix green in unit tests; webhook idempotency verified.
