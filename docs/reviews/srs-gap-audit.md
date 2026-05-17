# SRS Gap Audit — ScholarPath

**Date:** 2026-05-17
**Audited against:** `SRS_ScholarPath_Final (4).docx` (repo root) — functional requirements
FR-001 … FR-234, the role permission matrix, project scope, and the non-functional
requirements in section 5.
**Branch audited:** `integration`
**Method:** The SRS `.docx` text was extracted from `word/document.xml` and read in
full. Each requirement was then checked against the actual code — backend MediatR
slices under `server/src/ScholarPath.Application/`, controllers under
`server/src/ScholarPath.API/Controllers/`, domain entities under
`server/src/ScholarPath.Domain/`, and the frontend under `client/src/pages/` and
`client/src/routes/router.tsx`. Statuses are evidence-based, not inferred from
module trackers.

## Status legend

- **Implemented** — requirement is fully built and reachable end-to-end.
- **Partial** — core behaviour exists but a defined sub-requirement is missing,
  stubbed, or only partly wired.
- **Missing** — no implementation found.

---

## A. General platform scope and access control (FR-001 – FR-006)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-001 Gated platform covering all listed surfaces | Implemented | 17 controllers under `server/src/ScholarPath.API/Controllers/`; full route tree in `client/src/routes/router.tsx` | All pillars present. |
| FR-002 Home Page only public page | Implemented | `router.tsx` — `/` (`Home`) in `PublicLayout`; all feature routes wrapped in `RequireAuth` | |
| FR-003 Auth required for everything else | Implemented | `RequireAuth`/`RequireRole` (`client/src/routes/RequireAuth.tsx`); `[Authorize]` on every controller except webhook/diagnostics ping | |
| FR-004 Role names Student/Company/Consultant/Admin/Unassigned | Implemented | `AccountStatus` enum (`Domain/Enums/Enums.cs`); role strings used throughout `[Authorize(Roles=...)]` | SuperAdmin also exists as an extra role. |
| FR-005 Unassigned as valid onboarding state | Implemented | `AccountStatus.Unassigned`; `RegisterCommandHandler` sets it on new accounts | |
| FR-006 Gated-access prompts for Guests | Implemented | `RequireAuth.tsx` redirects unauthenticated users to login | |

## B. Registration, authentication, onboarding (FR-007 – FR-027)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-007 Email/password registration | Implemented | `Auth/Commands/Register/`; `POST /api/auth/register` | |
| FR-008 Google SSO | **Partial** | `POST/GET /api/auth/google/authorize|callback`; `SsoLoginCommandHandler` | **SSO is stubbed.** `ISsoService` is bound to `StubSsoService` in `Infrastructure/DependencyInjection.cs:85`; `StubServices.cs` `ExchangeGoogleCodeAsync` returns a hardcoded fake user. No real Google token exchange. |
| FR-009 Microsoft SSO | **Partial** | Same controller actions; `StubSsoService.ExchangeMicrosoftCodeAsync` | Same as FR-008 — stubbed. |
| FR-010 Enter onboarding after registration | Implemented | `OnboardingWizard` route `/onboarding`; `SelectRoleCommand` | |
| FR-011 New users created Unassigned | Implemented | `RegisterCommandHandler` — `AccountStatus.Unassigned`, `IsOnboardingComplete=false` | |
| FR-012 Choose Student/Company/Consultant in onboarding | Implemented | `Auth/Commands/SelectRole/`; `POST /api/auth/select-role` | |
| FR-013 Student onboarding activates immediately | Implemented | `SelectRoleCommandHandler` | |
| FR-014 Company onboarding → admin review | Implemented | `SelectRoleCommandHandler`; `Admin/.../ReviewOnboarding`; `GET /api/admin/onboarding-queue` | |
| FR-015 Consultant onboarding → admin review | Implemented | Same onboarding-queue flow | |
| FR-016 Student→Consultant upgrade request | Implemented | `UpgradeRequest` entity; upgrade queue (`GET /api/admin/upgrade-queue`) | Verified via Admin upgrade-queue endpoints; submission path exists through the upgrade-request entities. |
| FR-017 Admin approval of upgrade | Implemented | `Admin/Commands/ReviewUpgradeRequest/`; `POST /api/admin/upgrade-queue/{id}/review` | |
| FR-018 Preserve Student history after Consultant approval | Implemented | Single `ApplicationUser` + `ActiveRole`; no record deletion on upgrade | |
| FR-019 In-session role switcher | Implemented | `Auth/Commands/SwitchRole/`; `POST /api/auth/switch-role`; `ActiveRole` on `ApplicationUser` | |
| FR-020 Adapt UI to active mode | Implemented | `ActiveRole` drives role-based routing in `router.tsx` | |
| FR-021 Password rules (8 char, upper, digit, special) | Implemented | `RegisterCommandValidator.cs:14-19` — all four rules enforced | |
| FR-022 Access + refresh token session | Implemented | `TokenService.cs`; `RefreshToken` entity | |
| FR-023 Remember Me extends refresh lifetime | Implemented | `RememberMe` flag on `RegisterCommand`/`LoginCommand`, passed to `tokenService.IssueTokens` | |
| FR-024 Password reset via email link | Implemented | `Auth/Commands/ForgotPassword` + `ResetPassword`; `PasswordResetToken` entity | |
| FR-025 Invalidate refresh tokens after reset | Implemented | `ResetPasswordCommandHandler` | |
| FR-026 Account lockout after failed logins | Implemented | `LoginCommandHandler.cs:19-65` — 5 attempts / 15-min window → 30-min lockout | |
| FR-027 Onboarding/account status as route-guard state | Implemented | `AccountStatus`, `IsOnboardingComplete` on `ApplicationUser`; consumed by `RequireAuth` | |

## C. User profiles and role-specific data (FR-028 – FR-033)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-028 Base user profile | Implemented | `UserProfile` entity; `ProfileController` `GET/PATCH /api/profiles/me` | |
| FR-029 Student profile attributes | Implemented | `UserProfile` — `AcademicLevel`, `FieldOfStudy`, `Gpa`, `PreferredCountriesJson`, etc. | |
| FR-030 Company profile attributes | Implemented | `UserProfile` — `OrganizationLegalName`, `OrganizationVerificationStatus`, etc. | |
| FR-031 Consultant profile attributes | Implemented | `UserProfile` — `SessionFeeUsd`, `ExpertiseTagsJson`, `StripeConnectAccountId`, etc. | |
| FR-032 Profile completeness indicator | Implemented | `Profile/ProfileCompletenessCalculator.cs`; `ProfileCompletenessPercent` field | |
| FR-033 Edit photo/bio/language/details | Implemented | `Profile/Commands/UpdateProfile` + `UploadProfilePhoto`; `POST /api/profiles/me/photo` | |

## D. Scholarship discovery and data (FR-034 – FR-046)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-034 Browse/search listings | Implemented | `Scholarships/Queries/GetScholarshipsQuery`; `GET /api/scholarships`; full-text search migration `20260501180102_AddFullTextSearchToScholarships` | |
| FR-035 Filter by category/country/deadline/funding/level/tags | Implemented | `GetScholarshipsQuery` filter params; `Scholarship` fields support all listed facets | |
| FR-036 Sort by relevance/deadline/newest/recommended | Implemented | `GetScholarshipsQuery` sort options | |
| FR-037 In-app + external-URL listing types | Implemented | `ListingMode` enum (`InApp`/`ExternalUrl`); `Scholarship.Mode` | |
| FR-038 Company creates in-app listings | Implemented | `Scholarships/Commands/CreateScholarship`; `POST /api/scholarships` `[Authorize(Roles=Company)]` | |
| FR-039 External-URL listings restricted to Admin | Implemented | `CreateScholarshipCommandHandler` (admin path); `Scholarship.CreatedByAdminId` | |
| FR-040 Mandatory core listing fields | Implemented | `Scholarship` required fields; `UpdateScholarshipCommandValidator` | |
| FR-041 In-app listings define form fields | Implemented | `Scholarship.ApplicationFormSchemaJson` | |
| FR-042 In-app listings define required documents | Implemented | `Scholarship.RequiredDocumentsJson` | |
| FR-043 Block deadline violating min lead-time | Implemented | `CreateScholarshipCommand` / `UpdateScholarshipCommandValidator` deadline rule | |
| FR-044 Tag listings for discovery | Implemented | `Scholarship.TagsJson` | |
| FR-045 Students bookmark listings | Implemented | `SavedScholarship` entity; `Scholarships/Commands/BookmarkToggle`; `POST /api/scholarships/{id}/bookmark` | |
| FR-046 Notify on bookmarked deadlines/drafts | **Partial** | `DeadlineReminderJob` is **registered but a stub** — `Jobs/Jobs.cs:12-19` only logs `"DeadlineReminderJob tick (stub)"`. Scheduled in `Program.cs:235` (`Cron.Daily(9)`). | No reminder is actually sent. |

## E. Application management and tracking (FR-047 – FR-062)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-047 Submit in-app application | Implemented | `Applications/Commands/StartApplication` + `SubmitApplication`; `POST /api/applications`, `PUT /api/applications/{id}/submit` | |
| FR-048 Save application as draft | Implemented | `StartApplicationCommandHandler` creates `ApplicationStatus.Draft` | |
| FR-049 Attach documents from a document vault | **Partial** | `ApplicationTracker.AttachedDocumentsJson`; `SubmitApplicationCommandHandler` checks documents attached | **There is no document vault.** No `StudentDocument`/vault entity, no `DbSet`, no vault controller. Documents are referenced as a JSON blob only — FR-216 (vault) is Missing, so the "from the vault" part of this requirement is not met. |
| FR-050 In-app application status lifecycle | Implemented | `ApplicationStatus` enum; `Applications/Common/ApplicationStateMachine.cs` | |
| FR-051 Status-history trail | Implemented | `ApplicationTrackerChild` (`ChildType="StatusHistory"`); `Applications/EventHandlers/ApplicationStatusHistoryEventHandler.cs` | |
| FR-052 Company reviews & updates status | Implemented | `Applications/Commands/ReviewApplication`; `POST /api/applications/{id}/review` `[Authorize(Roles=Company)]` | |
| FR-053 External-URL listings generate self-track records | Implemented | `ApplicationMode.External`; external states `Intending/Applied/WaitingResult` | |
| FR-054 Redirect Student to external URL | Implemented | `Scholarship.ExternalApplicationUrl`; `StartApplicationCommandHandler` blocks manual apply on external listings | |
| FR-055 Manual update of self-tracked status | Implemented | `Applications/Commands/UpdateExternalStatus`; `PATCH /api/applications/{id}/external-status` | |
| FR-056 Personal notes on external record | Implemented | `ApplicationTracker.PersonalNotes` | |
| FR-057 One active application per scholarship | Implemented | `StartApplicationCommandHandler.cs:50-59` duplicate-active guard | |
| FR-058 Withdraw while eligible | Implemented | `Applications/Commands/WithdrawApplication`; terminal-state guard via `ApplicationTracker.IsActive` | |
| FR-059 Reapply after withdrawal if still open | Implemented | `StartApplicationCommandHandler` — withdrawn apps excluded from active check + scholarship must be `Open` | |
| FR-060 Lock accepted/rejected as read-only | Implemented | `ApplicationTracker.IsReadOnly` (Accepted/Rejected/Withdrawn) | |
| FR-061 Timeline + next-step guidance | Implemented | `client/src/pages/student/StudentApplicationDetail.tsx`; status-history children | |
| FR-062 Remind on deadlines/drafts/status changes | **Partial** | Status-change notifications fire via event handlers; deadline/draft reminders depend on the **stub** `DeadlineReminderJob` | |

## F. Company-side review and payment model (FR-063 – FR-069)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-063 Company dashboard for listings/reviews | Implemented | `client/src/pages/company/Dashboard.tsx`, `CompanyScholarships.tsx`, `ApplicationsReview.tsx` | |
| FR-064 Company sees only own listings' applications | Implemented | `Applications/Queries/GetCompanyApplications` filters by owner company | |
| FR-065 Company reviews materials & updates decision | Implemented | `ReviewApplicationCommandHandler` | |
| FR-066 Stripe for Company payment flows | Implemented | `CompanyReviewPayment` entity; `CompanyReviews/Commands/CaptureCompanyReviewPayment` + `RefundCompanyReview` | |
| FR-067 Company revenue tied to reviewing applications | Implemented | `Scholarship.ReviewFeeUsd`; `CompanyReviewPayment` per `ApplicationTrackerId` | |
| FR-068 Configurable Company review fee rules | Implemented | `Scholarships/Commands/ConfigureReviewFee`; `POST /api/scholarships/{id}/review-fee` | |
| FR-069 Payment/settlement records for Company reviews | Implemented | `CompanyReviewPayment` with profit-share + payee amounts | |

## G. Company review and rating (FR-070 – FR-075)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-070 Student rates Company after final outcome | Implemented | `CompanyReviews/Commands/SubmitCompanyRating`; `POST /api/company-reviews` `[Authorize(Roles=Student)]` | |
| FR-071 Associate rating with Student + application | Implemented | `CompanyReview` — `StudentId`, `ApplicationTrackerId` | |
| FR-072 Display Company rating on profile/pages | Implemented | `CompanyReviews/Queries/GetCompanyRatings`; `GET /api/companies/{id}/reviews` | |
| FR-073 Average rating + total count | Implemented | `GetCompanyRatingsQueryHandler` aggregates | |
| FR-074 Prevent duplicate Company rating per application | Implemented | `SubmitCompanyRatingCommandHandler` / one-review-per-`ApplicationTrackerId` | |
| FR-075 Admin moderates/hides/removes Company reviews | **Missing** | `CompanyReview.IsHiddenByAdmin` + `AdminNote` fields exist, but **no command or endpoint sets them.** No `HideCompanyReview`/moderation slice; `AdminController` has no review-moderation action. | Data model is ready; the moderation action is unbuilt. |

## H. Consultant booking, cancellation, refund, payments (FR-076 – FR-095)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-076 Consultant directory searchable | Implemented | `client/src/pages/student/ConsultantsBrowse.tsx`; consultant profile data | |
| FR-077 Consultant profile (expertise/credentials/availability/reviews/fee) | Implemented | `ConsultantDetail.tsx`; `UserProfile` consultant fields; `ConsultantAvailability` | |
| FR-078 Request booking by selecting slot | Implemented | `ConsultantBookings/Commands/RequestBooking`; `POST /api/consultants/{id}/book` | |
| FR-079 Stripe processes session payments | Implemented | `Payments/Commands/CreatePaymentIntent`; `StripeService.cs` | |
| FR-080 Payment hold on booking request | Implemented | `RequestBookingCommandHandler` + `PaymentStatus.Held` | |
| FR-081 Capture only on Consultant accept | Implemented | `AcceptBookingCommandHandler` + `Payments/Commands/CapturePaymentIntent` | |
| FR-082 Release hold on reject/expiry | Implemented | `RejectBookingCommandHandler`; `SessionExpiryJob` (`Program.cs:232`, every 15 min) | |
| FR-083 Consultant accept/reject within window | Implemented | `AcceptBooking` / `RejectBooking` commands; expiry job enforces window | |
| FR-084 Record booking details | Implemented | `ConsultantBooking` — session type, duration, `MeetingUrl`, `CancellationReason`, no-show flags | |
| FR-085 Cancel before acceptance → full refund | Implemented | `CancelBookingCommandHandler`; `RefundCalculatorService.cs`; `CancellationReason.StudentCancelledBeforeAcceptance` | |
| FR-086 Full refund on reject/expiry | Implemented | `RefundCalculatorService` + reject/expiry handlers | |
| FR-087 Full refund: cancel >24h after acceptance | Implemented | `RefundCalculatorService`; `CancellationReason.StudentCancelledMoreThan24HoursBefore` | |
| FR-088 50% refund: cancel <24h after acceptance | Implemented | `RefundCalculatorService`; `CancellationReason.StudentCancelledLessThan24HoursBefore` | |
| FR-089 Full refund: Consultant cancels after accept | Implemented | `RefundCalculatorService`; `CancellationReason.ConsultantCancelledAfterAcceptance` | |
| FR-090 Full refund: Consultant no-show | Implemented | `MarkNoShowCommandHandler`; `CancellationReason.ConsultantNoShow` | |
| FR-091 No refund: Student no-show | Implemented | `MarkNoShowCommandHandler`; `CancellationReason.StudentNoShow` | |
| FR-092 Both parties view booking/refund/payment status | Implemented | `StudentBookingDetails.tsx`, `ConsultantBookingDetails.tsx` | |
| FR-093 Students rate completed sessions | Implemented | `ConsultantBookings/Commands/SubmitConsultantRating`; `POST /api/bookings/{id}/rating` | |
| FR-094 Auto-suspend low-rated consultants pending Admin review | Implemented | `SubmitConsultantRatingCommandHandler:~105` — sets `AccountStatus.Suspended` when avg over last 20 sessions falls below threshold | |
| FR-095 Log Stripe webhooks for idempotency | Implemented | `StripeWebhookEvent` entity (unique `StripeEventId`); `Payments/Commands/ProcessStripeWebhook` | |

## I. Consultant review and rating (FR-096 – FR-101)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-096 Student rates Consultant after completed session | Implemented | `SubmitConsultantRatingCommandHandler` — requires `BookingStatus.Completed` | |
| FR-097 Associate rating with Student + booking | Implemented | `ConsultantReview` — `StudentId`, `BookingId` | |
| FR-098 Display Consultant rating on profile/pages | Implemented | Consultant profile + booking-detail pages | |
| FR-099 Average rating + total count | Implemented | Aggregation in consultant rating query path | |
| FR-100 Prevent duplicate Consultant rating per booking | Implemented | `SubmitConsultantRatingCommandHandler` one-review-per-`BookingId` guard | |
| FR-101 Admin moderates/hides/removes Consultant reviews | **Missing** | `ConsultantReview.IsHiddenByAdmin` + `AdminNote` fields exist, but **no command/endpoint sets them.** Same gap as FR-075. | |

## J. Community (FR-102 – FR-108)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-102 Community forum for authenticated users | Implemented | `CommunityController` `[Authorize]`; `client/src/pages/community/` | |
| FR-103 Create posts & replies in categories | Implemented | `Community/Commands/CreatePost` + `CreateReply`; `ForumPost` (root + reply) | |
| FR-104 Upvote/downvote posts & replies | Implemented | `Community/Commands/ToggleVote`; `ForumVote` entity | |
| FR-105 Prevent voting on own content | Implemented | `ToggleVoteCommand` handler self-vote guard | |
| FR-106 Flag inappropriate content | Implemented | `Community/Commands/FlagPost`; `ForumFlag` entity | |
| FR-107 Auto-hide after 3+ valid flags → moderation queue | Implemented | `Community/EventHandlers/PostAutoHiddenEventHandler.cs`; `ForumPost.IsAutoHidden`; `GET /api/community/admin/flagged` | |
| FR-108 Forum posts may contain attachments | Implemented | `ForumPostAttachment` entity | |

## K. Chat (FR-109 – FR-112)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-109 One-to-one real-time chat with status | Implemented | `Infrastructure/Hubs/Hubs.cs` `ChatHub` (SignalR); `Chat/Commands/SendMessage`; `client/src/pages/chat/Chat.tsx` | |
| FR-110 Online/offline presence indicators | Implemented | `ChatHub.OnConnectedAsync`/`OnDisconnectedAsync` broadcast `UserOnline`/`UserOffline` | |
| FR-111 Persist chat messages / history | Implemented | `ChatMessage` entity; `Chat/Queries/GetMessages` | |
| FR-112 Block users from initiating chat | Implemented | `Chat/Commands/BlockUser`; `UserBlock` entity; `POST /api/chat/blocks` | |

## L. AI features (FR-113 – FR-121)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-113 Personalized recommendations | Implemented | `AI/Commands/GenerateRecommendations`; `LocalAiService`/`OpenAiService`; `GET /api/ai/recommendations` | |
| FR-114 Match score + explanation per recommendation | Implemented | `RecommendationItemDto` — `MatchScore`, `ExplanationEn/Ar` | |
| FR-115 Refresh recommendations when profile changes | Implemented | `POST /api/ai/recommendations` regenerates on demand | |
| FR-116 Eligibility checker | Implemented | `AI/Commands/CheckEligibility`; `POST /api/ai/eligibility/{scholarshipId}`; `client/.../EligibilityChecker.tsx` | |
| FR-117 Returns Eligible/Partially/Not Eligible per-criterion | **Partial** | `EligibilityDto` returns per-criterion `Match` strings (`yes`/`no`/`partial`/`unknown`) + a free-text `Summary` (`LocalAiService.cs:115-155`). | The SRS mandates an explicit overall **Eligible / Partially Eligible / Not Eligible** verdict — the code produces a prose summary, not that tri-state classification. |
| FR-118 Suggest profile improvements | **Partial** | Eligibility summary mentions reviewing "partial/no items"; no dedicated structured profile-improvement suggestions. | Overlaps FR-232 (also Partial). |
| FR-119 AI chatbot | Implemented | `AI/Commands/AskChatbot`; `POST /api/ai/chat`; `client/.../Chatbot.tsx` | |
| FR-120 Retain chatbot history per session | Implemented | `AiInteraction.SessionId`; `AI/Queries/GetMyInteractions` | |
| FR-121 AI-generated disclaimer in every response | Implemented | `LocalAiService.cs:20` / `OpenAiService.cs:31` constant `Disclaimer`; returned in every DTO (`AiDtos.cs`) | |

## M. Resources hub (FR-122 – FR-137)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-122 Resources Hub for authenticated users | Implemented | `ResourcesController`; `client/src/pages/student/StudentResources.tsx` | |
| FR-123 Articles only for v1 | Implemented | `ResourceType` enum defaults to `Article`; `Resource.Type` | Enum also has Guide/Checklist/VideoLink but Article is the produced type. |
| FR-124 Consultant/Company/Admin publish articles | Implemented | `Resources/Commands/CreateResource`; `POST /api/resources` `[Authorize(Roles=Consultant,Company,Admin)]` | |
| FR-125 Article publishing subject to Admin moderation | Implemented | `Resources/Commands/ApproveResource` + `RejectResource`; `ResourceStatus` workflow | |
| FR-126 Required article fields | Implemented | `Resource` — title, description, author, `PublishedAt`, content/link | |
| FR-127 Search/filter articles | Implemented | `Resources/Queries/SearchResources` | |
| FR-128 Admin features articles on landing | Implemented | `Resources/Commands/FeatureResource`; `GET /api/resources/featured` | |
| FR-129 Validate community post submissions | Implemented | `CreatePostCommand` validator (non-empty, length, category) | |
| FR-130 Validate reply submissions | Implemented | `CreateReplyCommand` validator | |
| FR-131 Clear validation errors for post/reply | Implemented | FluentValidation pipeline returns 400 with messages | |
| FR-132 Flagging rules (auth-only, no self-flag, no dup, valid reason) | Implemented | `FlagPostCommand` handler enforces all four | |
| FR-133 Chat-block rules enforced | Implemented | `UserBlock`; `SendMessageCommand` block check | |
| FR-134 Unblock user | Implemented | `Chat/Commands/UnblockUser`; `DELETE /api/chat/blocks/{userId}` | |
| FR-135 Validate article submissions | Implemented | `CreateResourceCommand` validator (required fields, links) | |
| FR-136 Reject invalid article submissions with errors | Implemented | `CreateResource`/`UpdateResource` validators | |
| FR-137 Article hidden until validation + moderation pass | Implemented | `ResourceStatus` (`Draft`→`PendingReview`→`Published`); only `Published` visible | |

## N. Notifications (FR-138 – FR-152)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-138 In-app + email delivery | Implemented | `NotificationChannel` enum; `Infrastructure/Services/NotificationDispatcher.cs` + `MailKitEmailService.cs` | |
| FR-139 Consistent delivery across all roles | Implemented | `NotificationDispatcher` keyed by recipient + event type | |
| FR-140 Notify on application submission | Implemented | `NotificationType.ApplicationSubmitted`; `ApplicationSubmittedEvent` handler | |
| FR-141 Notify Students on status changes | Implemented | `NotificationType.ApplicationStatusChanged` | |
| FR-142 Notify Companies on new applications | Implemented | Application event handlers dispatch to company | |
| FR-143 Notify Company on rating received | Implemented | `NotificationType.CompanyRatingReceived` | |
| FR-144 Notify Companies of review-payment events | Implemented | `NotificationType.CompanyReviewPaymentSuccess`/`CompanyReviewRefunded` | |
| FR-145 Notify Students of booking outcomes/refunds | Implemented | `NotificationType.BookingConfirmed`/`Rejected`/`Expired`/`Cancelled` | |
| FR-146 Notify Consultants of reservation events | Implemented | `NotificationType.BookingRequested` + booking lifecycle types | |
| FR-147 Notify Consultant on rating received | Implemented | `NotificationType.ConsultantRatingReceived` | |
| FR-148 Notify Consultants of payment/payout events | Implemented | `NotificationType.PaymentSuccess`/`PayoutInitiated`/`PayoutCompleted`/`PayoutFailed` | |
| FR-149 Notify Admins of pending approvals | Implemented | `NotificationType.AdminApprovalRequired` | |
| FR-150 Admin broadcast announcements | Implemented | `Admin/Commands/SendBroadcast`; `POST /api/admin/broadcasts` | |
| FR-151 Mark notifications read/unread | Implemented | `Notifications/Commands/MarkAsRead` + `MarkAllAsRead`; `Notification.IsRead` | |
| FR-152 Email to registered address | Implemented | `MailKitEmailService` sends to `ApplicationUser.Email` | |

## O. Admin portal and platform oversight (FR-153 – FR-176)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-153 Admin portal | Implemented | `AdminController` `[Authorize(Roles=Admin,SuperAdmin)]`; `AdminLayout` + `client/src/pages/admin/` | |
| FR-154 View/search/filter users | Implemented | `Admin/Queries/SearchUsers`; `GET /api/admin/users` | |
| FR-155 Approve/reject Company onboarding | Implemented | `Admin/Commands/ReviewOnboarding` | |
| FR-156 Approve/reject Consultant onboarding | Implemented | Same onboarding-queue flow | |
| FR-157 Approve/reject upgrade requests | Implemented | `Admin/Commands/ReviewUpgradeRequest` | |
| FR-158 Activate/suspend/deactivate/delete accounts | Implemented | `Admin/Commands/SetUserStatus` + `SoftDeleteUser`; `POST /api/admin/users/{id}/status`, `DELETE /api/admin/users/{id}` | |
| FR-159 Manage scholarship/article/category/moderation/featured content | Implemented | `GetScholarshipsForModeration`, resource moderation, `Community/Commands/CreateCategory`, feature commands | |
| FR-160 Moderate flagged content + record actions | Implemented | `Community/Commands/DismissPostFlags`; `POST /api/community/admin/posts/{id}/remove` | |
| FR-161 Analytics dashboard | Implemented | `Admin/Queries/GetAnalyticsOverview`, `GetUserGrowth`, `GetApplicationFunnel`; `client/.../AnalyticsPage.tsx` | |
| FR-162 CSV export for analytics/reports | **Missing** | No CSV export found — no `text/csv` content type, no export endpoint, no export query anywhere in `Application` or `API`. | Priority 4, but entirely unbuilt. |
| FR-163 Manage payment fee configuration | Implemented | `PlatformSettings/Commands/UpdatePlatformSetting`; `PlatformSetting` entity; `PUT /api/admin/settings` | |
| FR-164 Configure profit-share % per payment type | Implemented | `ProfitShare/Commands/SetProfitShareConfig`; `PUT /api/admin/profit-share/{paymentType}` | |
| FR-165 CRUD/activate/archive financial config without code change | Implemented | `ProfitShareConfig` entity (effective dates); `SetProfitShareConfig` | |
| FR-166 Admin rules as platform-level defaults | Implemented | `ProfitShareConfigResolver.cs` | |
| FR-167 Calculation preview before save | **Partial** | `ProfitShareCalculator.cs` exists and `client/.../AdminProfitShare.tsx` renders config; an explicit pre-save gross/fee/share/net **preview** is not clearly wired. | Priority 4 — verify against the Admin Profit Share UI. |
| FR-168 Validate fee/percentage inputs | Implemented | `SetProfitShareConfigCommand` validator; `ConfigureReviewFeeCommandValidator` | |
| FR-169 Effective start/end dates for config | Implemented | `ProfitShareConfig.EffectiveFrom`/`EffectiveTo` | |
| FR-170 Only one active rule per payment type | Implemented | `SetProfitShareConfigCommandHandler` closes the prior active config | |
| FR-171 Audit trail for financial config changes | Implemented | `AuditAction.ConfigChanged`; `AuditLog` before/after JSON | |
| FR-172 Restrict financial config to Admins | Implemented | `ProfitShareController` `[Authorize(Roles=Admin,SuperAdmin)]` | |
| FR-173 View current + historical financial rules | Implemented | `ProfitShare/Queries/GetActiveProfitShareConfigs` + `GetProfitShareHistory` | |
| FR-174 Search/filter financial config records | **Partial** | `GetProfitShareHistory` lists history; explicit filter by status/effective-date/last-updated not confirmed in the query. | Priority 4. |
| FR-175 Simulate sample transaction values before activating | **Partial** | `ProfitShareCalculator` can compute values; a dedicated simulate-before-activate path is not clearly exposed. | Overlaps FR-167. |
| FR-176 Prevent deletion of used financial rule (archive/deactivate only) | Implemented | `ProfitShareConfig` is closed via `EffectiveTo`, never deleted; configs are append-only | |

## P. Data integrity, audit, alignment (FR-177 – FR-182)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-177 Audit records for critical actions | Implemented | `[Auditable]` attribute on commands; `AuditLog` entity; `Infrastructure/Services/AuditService.cs` | |
| FR-178 Login-attempt records | Implemented | `LoginAttempt` entity; written by `LoginCommandHandler` | |
| FR-179 Explicit role-upgrade request records | Implemented | `UpgradeRequest` + `UpgradeRequestFile`/`UpgradeRequestLink` entities | |
| FR-180 Preserve student data on Consultant approval | Implemented | Single-account model; no data deletion on role change | |
| FR-181 Unassigned accounts exist before subclass profile | Implemented | `ApplicationUser` separate from `UserProfile`; profile created later | |
| FR-182 Data export + deletion-request tracking | Implemented | `UserDataRequest` entity; `Audit/Commands/RequestDataExport` + `RequestDataDelete`; `DataPrivacyController` | |

## Q. Payment processing, refunds, settlement (FR-183 – FR-200)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-183 Centralized payment processing | Implemented | `PaymentsController`; `Payment` entity covers both payment types | |
| FR-184 Stripe as primary gateway | Implemented | `Infrastructure/Services/StripeService.cs` | |
| FR-185 Stripe PaymentIntent per booking | Implemented | `Payments/Commands/CreatePaymentIntent`; `Payment.StripePaymentIntentId` | |
| FR-186 Payment hold on booking request | Implemented | `RequestBookingCommandHandler`; `PaymentStatus.Held` | |
| FR-187 Capture after Consultant accepts | Implemented | `CapturePaymentIntentCommand` | |
| FR-188 Release hold on decline/expiry | Implemented | Reject handler + `SessionExpiryJob` | |
| FR-189 Stripe Connect onboarding for Consultants | Implemented | `Payments/Commands/CreateConnectAccount`; `StripeService.CreateConnectAccountAsync` + `CreateConnectOnboardingLinkAsync` | |
| FR-190 Retain service fee, transfer net | Implemented | `Payment.ProfitShareAmountCents`/`PayeeAmountCents`; `StripePayoutJob` | |
| FR-191 Company payment/refund for review services | Implemented | `CompanyReviewPayment`; `RefundCompanyReview` | |
| FR-192 Record full payment details | Implemented | `Payment` entity — id, amount, status, capture/refund status, payer/payee, dates, related record | |
| FR-193 Full + partial refunds per policy | Implemented | `RefundPaymentCommand`; `RefundCalculatorService`; `PaymentStatus.PartiallyRefunded` | |
| FR-194 Payment confirmation/receipt after capture | Implemented | `NotificationType.PaymentSuccess`; email via `MailKitEmailService` | |
| FR-195 Validate + log Stripe webhooks (idempotent) | Implemented | `WebhooksController` (signature verification); `StripeWebhookEvent`; `ProcessStripeWebhookCommand` | |
| FR-196 Payment/settlement history views | Implemented | `Payments/Queries/GetPayments`, `GetMyPayouts`; `GET /api/payments`, `/api/payments/payouts` | |
| FR-197 Apply active financial config on transaction | Implemented | `ProfitShareConfigResolver` used at payment creation | |
| FR-198 Use stored financial values for original transaction | Implemented | `Payment` stores `ProfitShareAmountCents` snapshot | |
| FR-199 Recalculate refund impact on original basis | Implemented | `RefundCalculatorService` works off the original `Payment` | |
| FR-200 Preserve applied fee/share/payout per transaction | Implemented | `Payment` cents fields are write-once snapshots | |

## R. Portal profit share (FR-201 – FR-211)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-201 Retain configurable profit share from Consultant payments | Implemented | `ProfitShareCalculator.cs`; `Payment.ProfitShareAmountCents` | |
| FR-202 Default Consultant share 10% unless overridden | Implemented | `ProfitShareConfigResolver` default; `ProfitShareConfig.Percentage` | |
| FR-203 Retain profit share from Company review payments | Implemented | `CompanyReviewPayment.ProfitShareAmountUsd` | |
| FR-204 Default Company share 10% unless overridden | Implemented | Same resolver, `PaymentType.CompanyReview` | |
| FR-205 Record gross/share%/share amount/net/refund impact/payout status per transaction | Implemented | `Payment` + `CompanyReviewPayment` fields | |
| FR-206 Recalculate share on refund | Implemented | `ProcessStripeWebhookCommand` refund branch + `RefundCalculatorService` | |
| FR-207 Configure share % separately per payment type | Implemented | `SetProfitShareConfig` keyed by `PaymentType` | |
| FR-208 Expose share/net to Admin reporting | Implemented | `ProfitShare/Queries/GetProfitShareAnalytics` | |
| FR-209 Calculate platform share/net/refund per config | Implemented | `ProfitShareCalculator` | |
| FR-210 Preserve financial calculation snapshots | Implemented | Write-once `Payment` cents fields | |
| FR-211 Expose gross/share/net in financial audit views | Implemented | `GetProfitShareAnalytics`; `client/.../AdminProfitShare.tsx` | |

## S. Gap-closure requirements, post-audit (FR-212 – FR-234)

| Req | Status | Evidence | Notes |
|-----|--------|----------|-------|
| FR-212 Logout invalidates tokens + redirect Home | Implemented | `Auth/Commands/Logout`; `POST /api/auth/logout` `[Authorize]` | |
| FR-213 Auto-refresh expired access token | Implemented | `Auth/Commands/RefreshToken`; `POST /api/auth/refresh` | |
| FR-214 Terminate session on refresh-token expiry | Implemented | `RefreshTokenCommandHandler` rejects expired/invalid tokens | |
| FR-215 Verification email after creation, required before onboarding | **Missing** | `RegisterCommandHandler.cs:43` sets `EmailConfirmed = false`, but **no verification email is sent and `EmailConfirmed` is never enforced** before onboarding. No verification command/endpoint exists. | Priority 4. The field exists but the flow is absent — onboarding proceeds without verification. |
| FR-216 Document vault (upload/view/organize/delete) | **Missing** | No `StudentDocument`/document-vault entity, no `DbSet`, no controller. Applications reference documents only as `ApplicationTracker.AttachedDocumentsJson`. | Priority 5 — a flagged gap that is still unbuilt. Directly weakens FR-049. |
| FR-217 Mark no-show + trigger refund policy | Implemented | `ConsultantBookings/Commands/MarkNoShow`; `POST /api/bookings/{id}/no-show` | Manual marking is built; `CompletionJob` covers booking completion. |
| FR-218 Unblock previously blocked user | Implemented | `Chat/Commands/UnblockUser` (also satisfies FR-134) | |
| FR-219 Personal data export with downloadable copy | Implemented | `Audit/Commands/RequestDataExport`; `DataExportJob`; `UserDataRequest.DownloadUrl` | |
| FR-220 Account-deletion request with tracking workflow | Implemented | `Audit/Commands/RequestDataDelete` + `CancelDataDelete`; `DataDeleteJob`; `UserDataRequest.ScheduledProcessAt` (30-day) | |
| FR-221 View/download payment receipts + history | Implemented | `Payments/Queries/GetPayments`; `client/.../ConsultantEarnings.tsx`, `CompanyBilling.tsx` | |
| FR-222 Stripe Connect onboarding (redirect/callback/verify) | Implemented | `StripeService.CreateConnectOnboardingLinkAsync` (`account_onboarding`); `StripeConnectStatus`; `POST /api/payments/connect/onboard` | |
| FR-223 Admin audit-log viewer (searchable) | Implemented | `Admin/Queries/GetAuditLog`; `GET /api/admin/audit-log`; `client/.../AuditLogViewer.tsx` | |
| FR-224 Handle payment failure (notify, retry alt method, release after window) | **Partial** | `ProcessStripeWebhookCommand` handles `payment_intent.payment_failed` → sets `PaymentStatus.Failed` + `FailureReason`. **No retry-with-alternative-method flow and no configured max-retry release window.** | Priority 5 — failure is recorded but the retry UX is unbuilt. |
| FR-225 Edit own posts/replies within edit window | **Partial** | `Community/Commands/UpdatePost` allows editing posts and replies (replies are `ForumPost` rows). **No configured edit-window time limit is enforced.** | Priority 4. |
| FR-226 Delete own posts/replies | Implemented | `Community/Commands/DeletePost` | |
| FR-227 Edit published article with re-validation/moderation | Implemented | `Resources/Commands/UpdateResource`; `SubmitResourceForReview` re-enters moderation | |
| FR-228 Configure notification preferences per category/channel | **Missing** | `NotificationPreference` entity + `DbSet` exist, but **no command or endpoint to read/update preferences.** `NotificationController` has only get/unread-count/mark-read. | Priority 4 — data model ready, behaviour unbuilt. |
| FR-229 Reschedule accepted booking without re-payment | **Missing** | No `RescheduleBookingCommand` anywhere; `BookingsController` has accept/reject/cancel/no-show/availability/rating only. | Priority 3 (desirable). |
| FR-230 Auto-close scholarship when deadline passes | **Missing** | `ScholarshipStatus.Closed` exists but **no job closes listings on deadline.** No auto-close job is registered in `Program.cs:229-239`; the only scholarship-related job is the **stub** `DeadlineReminderJob`. Manual `ArchiveScholarshipCommand` exists, but nothing automatic. | Priority 5 — listings will keep accepting applications past deadline unless manually archived. (`StartApplicationCommandHandler` does require status `Open`, so a manually-closed listing blocks new applications.) |
| FR-231 Change registered email via confirmation link | **Missing** | No change-email command/endpoint; `AuthController` has no such action. | Priority 4. |
| FR-232 AI profile-improvement suggestions for eligibility/match | **Partial** | Eligibility summary hints at weak criteria; no structured per-scholarship improvement suggestions surfaced. | Priority 4 — overlaps FR-118. |
| FR-233 View past AI chatbot sessions | Implemented | `AI/Queries/GetMyInteractions`; `GET /api/ai/interactions` (filterable by session) | |
| FR-234 Consultant payout dashboard | Implemented | `Payments/Queries/GetMyPayouts`; `client/.../ConsultantEarnings.tsx` | |

---

## Non-functional requirements (SRS section 5) — spot check

These were not exhaustively load-tested; this is a build-presence check.

| Area | Status | Notes |
|------|--------|-------|
| 5.2 Scalability — Docker, Redis backplane, Blob storage | Implemented (infra present) | `Dockerfile.api`, `Dockerfile.client`, `docker-compose.yml`; SignalR + Redis wiring referenced in `Program.cs`. |
| 5.3 Security — password hashing, JWT, webhook signature verify | Implemented | ASP.NET Identity hasher (`StubServices` `PasswordHasher`); `TokenService`; Stripe signature verification in `WebhooksController`. |
| 5.3 Security — antivirus scan on file uploads | **Missing / not found** | No AV-scan step found in upload paths (`UploadProfilePhotoCommand`, resource/forum attachments). |
| 5.3 Security — encrypt sensitive fields at rest (nationality, financial) | **Not found** | `UserProfile.Nationality` and financial fields are stored as plain columns; no field-level encryption observed. |
| 5.6 Maintainability — Clean Architecture, Swagger, 70% coverage | Implemented (structure) | Clean layering (Domain/Application/Infrastructure/API); Swagger configured; `server/tests/ScholarPath.UnitTests/` present — coverage % not measured here. |

---

## Summary — most important gaps

**Fully missing (no implementation):**

1. **FR-216 Document vault (Priority 5).** Students have no place to upload/organize/
   delete personal documents. Applications only carry an `AttachedDocumentsJson` blob.
   This is a flagged gap-closure requirement and also undermines FR-049.
2. **FR-230 Scholarship auto-close on deadline (Priority 5).** No background job closes
   listings when the deadline passes. Listings stay `Open` until an admin manually
   archives them. (`DeadlineReminderJob` — the nearest job — is a logging stub.)
3. **FR-075 / FR-101 Admin moderation of Company and Consultant reviews.** The
   `IsHiddenByAdmin`/`AdminNote` fields exist on `CompanyReview` and `ConsultantReview`,
   but there is no command or endpoint to hide/remove a review.
4. **FR-228 Notification preferences (Priority 4).** `NotificationPreference` entity and
   `DbSet` exist, but there is no way for a user to read or change preferences.
5. **FR-215 Email verification (Priority 4).** `EmailConfirmed` is set to `false` at
   registration but no verification email is sent and verification is never required
   before onboarding.
6. **FR-162 CSV export (Priority 4).** No analytics/report export exists anywhere.
7. **FR-229 Booking reschedule (Priority 3)** and **FR-231 Change registered email
   (Priority 4)** — no commands or endpoints.

**Partial / stubbed (highest-risk):**

- **FR-008 / FR-009 SSO is stubbed.** `ISsoService` resolves to `StubSsoService`
  (`Infrastructure/DependencyInjection.cs:85`), which returns hardcoded fake Google/
  Microsoft identities. The auth-code endpoints exist and the login flow works against
  the stub, but **no real Google/Microsoft token exchange happens** — a real OAuth
  provider must be implemented and registered before release.
- **FR-046 / FR-062 Deadline & draft reminders.** `DeadlineReminderJob` is registered
  on a daily cron but its body only logs `"DeadlineReminderJob tick (stub)"` — no
  reminders are sent. `NotificationDispatcherJob` is likewise a stub (status-change
  notifications still fire synchronously via event handlers, so those are fine).
- **FR-117 Eligibility verdict.** The checker returns per-criterion `yes/no/partial/
  unknown` plus a prose summary, but not the explicit overall **Eligible / Partially
  Eligible / Not Eligible** classification the SRS mandates.
- **FR-224 Payment-failure handling.** Failures are recorded (`PaymentStatus.Failed`),
  but there is no retry-with-alternative-payment-method flow and no configured
  max-retry release window.
- **FR-225 Post edit window** — editing works, but no time-limited edit window is
  enforced.
- **FR-167 / FR-174 / FR-175** financial-config preview, filtering, and pre-activation
  simulation are only partially wired — worth confirming against the Admin Profit Share
  UI before sign-off.

**NFR gaps worth noting:** no antivirus scanning of file uploads and no at-rest
encryption of sensitive personal fields (nationality, financial data) were found —
both are explicit SRS section 5.3 security requirements.

**Overall:** the platform is broad and substantially complete — all 14 PB modules
have real, end-to-end implementations, and the payment/profit-share area (the largest
and riskiest) is notably thorough. The gaps are concentrated in the post-audit
gap-closure block (FR-212–FR-234) and in two pieces of integration work that are
stubbed rather than absent: **real SSO** and the **deadline/reminder jobs**. SSO being
a stub is the single most important item to flag, because the SRS scope and the role
matrix both treat Google/Microsoft sign-in as in-scope for v1.
