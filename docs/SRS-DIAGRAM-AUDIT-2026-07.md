# SRS Diagram & Conformance Audit — 2026-07-04

Cross-checked the Auth, Onboarding and Consultant module diagrams (PlantUML appendix)
and user stories against the **actual code** in `server/src`. Three read-only reviewers.

Two kinds of finding, kept strictly separate:
- **A. Documentation drift** — the *diagrams* are wrong, the code is right → **you edit the SRS/PlantUML**.
- **B. Code gap** — the *code* is missing spec behaviour → needs implementation (a decision, see §4).

---

## 1. User-story actor check ("no story initiated by System")

All three modules are **clean** — every use case is initiated by a human/external actor
(Guest / Registered User / Authenticated User / Student / Consultant / Admin / Email Service / IdP).

**One exception, in the Scholarships module (not these three):**
- **US-SCH-18** is written *"As a System …"*. This is the one story to rewrite with a
  real actor (Admin or Provider, depending on intent). Flagged for you to edit.

---

## 2. Auth module diagrams — 11 of 35 need edits

**Systemic issue:** the sequence diagrams show textbook REST codes (400 / 401 / 423 / 200 / 201),
but the code throws `ConflictException` → **409** on nearly every failure and returns
**204 No Content** on success for logout/reset/forgot/resend. Every code below is a diagram fix.

> ⚠️ Separate decision (see §4-C): the code using **409 for a bad login/lockout** is itself
> unconventional — 401/423 would be more correct. So for the Login/Refresh diagrams you can
> either fix the *diagram* to 409, or fix the *code* to 401/423. Decide before editing those two.

| # | Diagram | Severity | Fix |
|---|---------|----------|-----|
| 28 | Login (seq) | 🔴 | Add the post-password **account-status gate** (Suspended/Deactivated → "not active"). Codes → 409 for both locked & generic-error branches (code never returns 401/423). Label lockout "5 *consecutive* fails / 15 min → 30 min lock". |
| 29 | Google SSO (seq) | 🔴 | **SSO state is issued by the server** `authorize` endpoint (`ISsoStateStore.Issue`), not by the LoginPage. Insert an `SsoLoginCommandHandler` participant — exchange/find/link/issue all live in the handler, not the Controller. Resolve account by **external-login link first, then email** (anti-rebind), and show `AddExternalLoginAsync` on both branches. |
| 30 | Microsoft SSO (seq) | 🔴 | Same three fixes as #29. |
| 31 | Refresh Session (seq) | 🔴 | Add the **reuse-detection branch**: replaying a revoked token → `RevokeAllForUserAsync` (kills the whole family). Invalid/revoked → 409, not 401. Validation is done by `ITokenService.RotateRefreshTokenAsync`. |
| 25 | Register (seq) | 🟠 | Success is **200 OK** (not 201) and **returns a token pair** (auto sign-in) — add the `IssueTokens` step. Duplicate-email 409 branch is correct. |
| 26 | Verify Email (seq) | 🟠 | Real endpoint is **`POST /auth/verify-email`** with body **`{ userId, token }`** (not `GET ?token`). Invalid token → **409** (not 400). |
| 34 | Set New Password (seq) | 🟠 | Reset token lives in a **`PasswordResetTokens`** table (add participant), not on `ApplicationUser`. Add **SecurityStamp rotation** alongside revoke-all. Success → **204** (not 200). |
| 32 | Logout (seq) | 🟠 | Success → **204** (not 200). Revoke only when a refresh token is supplied. |
| 33 | Request Password Reset (seq) | 🟠 | Success → **204** (not 200). No-enumeration behaviour otherwise correct. |
| 27 | Resend Verification (seq) | 🟡 | Success → **204** (not 200). Silent no-op logic is correct. |
| 35 | Route Protection (seq) | 🟡 | Reorder so the **401 branch short-circuits before** the "get roles/permissions" lookup (unauth request never reaches role lookup). |
| 17 | Login (activity) | 🟡 | Optional: add the "Account active?" outcome (Suspended/Deactivated → contact support), matching #28. |

**Verified correct (no edit):** diagrams 1–12 (use cases), 14, 16, 20, 22, 23, 24.

---

## 3. Onboarding module diagrams — 5 of 27 need edits

| # | Diagram | Severity | Fix |
|---|---------|----------|-----|
| 25 | Request Consultant Upgrade (seq) | 🔴 | **Wrong controller/command/handler.** It goes through `upgradeRequestsApi.submitConsultantUpgradeRequest` → **`UpgradeRequestsController`** → **`SubmitConsultantUpgradeRequestCommandHandler`**, which creates a distinct **`UpgradeRequest`** entity — NOT `authApi`/`AuthController`/a flag on `ApplicationUser`. Rename all participants/messages accordingly. |
| 21, 22, 25 | Submit-role / upgrade (seq) | 🔴 | Remove the fictional **`H → Doc : store uploaded documents`** step. Documents are uploaded on an **earlier wizard step**; the handler only **validates that required document *types* are already present** (`db.Documents` presence check), it never stores docs. |
| 19 | Select First Role (seq) | 🟠 | Email-not-verified → **409 `ConflictException`** (not 403 `VerificationRequiredError` — that type doesn't exist). |
| 21, 22, 24 | Submit / resubmit (seq) | 🟠 | Split the error alt: **missing input fields → 422** (`ValidationException`), but **missing required document types → 409** (`ConflictException`). Currently both are shown as one 422. |
| 26 | Admin Assess Upgrade (seq) | 🟠 | On reject, decision/notes are stored on the **`UpgradeRequest`** entity (`Status=Rejected`, `ReviewerNotes`), **not** on `ApplicationUser`. Add an `UpgradeRequest` participant. "Student role remains active" is correct (approve grants a 2nd role, never removes Student). |

**Verified correct (no edit):** diagrams 1–4, 7, 9–13, 16, 23, 24 (flow), 27 (Switch Role — the only correct 403 in the set).

---

## 4. Consultant module — a CODE gap, not a diagram problem 🔴🔴

This is the big one. **The entire penalty / enforcement half of the Consultant Booking spec
is not implemented.** Discovery, the core booking state machine, 24h auto-expire, 24h+1h
reminders and the low-rating flag are all correct — but the reputation/access penalties were
never modelled. There is **no data structure** to hold them, so these FRs cannot pass without a
schema change (migration).

| FR | Spec | Status |
|----|------|--------|
| CBR-21..24 | Student booking-access status {Active, BookingBlocked} + BlockReason + BlockUntil; blocked student can't book | ❌ **Missing entirely** — no fields, no guard in `RequestBookingCommandHandler` |
| CBR-25..32 | No-show goes to **admin validation** before penalties; validated student no-show → 7-day block; validated consultant no-show → −40% rating; false student report → 14-day block; consultant who falsely reports → −70% rating | ❌ **No admin gate** — `MarkNoShow` finalizes terminal status immediately, 0 blocks, 0 deductions |
| CBR-15..20 | Cancel <24h: **student → 3-day block**, **consultant → −20% rating** | ⚠️ **Partial** — cancellation is *recorded* + refund split is correct, but block & deduction are never applied |
| CBR-33..39 | Penalties "deduct from current average directly" | ❌ **No mechanism** — consultant average is a *live aggregate* recomputed every read; there is nowhere to persist a deduction (it'd be recomputed away) |
| CBR-37 | Flag when avg of **last 20** reviews < 2.5 | ⚠️ **Bug** — only fires once a consultant has a *full 20* reviews; someone with 8 reviews at 1.2 is never flagged. Should be "up to the last 20". |
| CBR-06 | Timezone-aware slots shown to students | ⚠️ **Minor** — backend returns per-slot timezone; frontend `ConsultantDetail.tsx` doesn't render the label |

**Root cause:** penalties were built as *refund* rules (money) but the spec's *reputation/access*
penalties (rating deductions + student blocks + admin validation) were never modelled.

**Implementation order if we build it:**
1. Persisted **consultant average-rating snapshot** (mirror the `ScholarshipProviderAverageRating` pattern) — unblocks all deductions.
2. Student **`BookingAccessStatus` + BlockReason/BlockUntil** + request-time guard (CBR-21..24).
3. **Admin no-show validation gate** with 7/14-day blocks + 40%/70% deductions (CBR-25..32).
4. Wire **3-day block / 20% deduction** into cancellation (CBR-15..20).
5. Fix the **low-rating flag** to evaluate on <20 reviews (CBR-37) — small, standalone.
6. Render **slot timezone** in `ConsultantDetail` (CBR-06) — small, standalone.

Items **5 and 6 are small and safe** (no schema change) — can ship now.
Items **1–4 are a real feature** (migration + new admin command + UI) — needs your go/no-go.

---

## 5. Decisions (resolved 2026-07-04)

- **C-1 — Consultant penalties (§4 items 1–4): BUILD the full system now.** Schema migration +
  admin no-show validation flow + UI. Executed in batches (each: confirm → fix → adversarial
  verify → build/test → admin-merge → deploy). Build order per §4.
- **C-2 — Auth status codes: keep the code at 409, fix the DIAGRAMS to match.** No code change;
  the diagram edits in §2 (status-code column) are documentation fixes the team makes.
- **C-3 — Small consultant fixes (§4 items 5 & 6): SHIPPED** — flag now evaluates on ≥5 (not a
  full 20) reviews via new `LowRatingMinimumSampleSize` option; student slot list shows the
  timezone label via `formatTimeWithTz`. No schema change.
- **Docs:** the diagram edits in §2 and §3 are yours to make (they're in the SRS/PlantUML files).
  I can generate corrected `.puml` snippets for any diagram you want if that's faster.

---

## 6. Scholarships module diagrams (P1 + P2 + part3) — documentation edits

Reviewed all three diagram sets (P1: 60 diagrams UC-SCH-01..20; P2: 30 diagrams UC-APP-01..10;
part3: 27 diagrams UC-APP-11..19) against the current code. **The code is correct** (it was
audited & fixed in the earlier wave); these are all **diagram/SRS edits for you to make** —
except one real code bug (§6.4, already fixed). No `System`-actor user-story violations
(UC-SCH-18 = "Scheduled Job / Time Trigger"; UC-APP-17/18 = event-initiated, matching the
MediatR handlers — optionally fold those two into their triggering human use case as
`<<include>>` to fully honour the "no System actor" convention).

### 6.1 Systemic fixes (recur across all three parts — do these globally)
- **State-name drift → rename everywhere:** `"In Assessment"` → **`UnderReview`**; `"Submitted"` →
  **`Pending`**; `"Open"` (approved listing) is fine but the enum name is `Open`. Real enums:
  scholarships `Draft/Open/Closed/Archived/UnderReview`; applications `Draft/Pending/UnderReview/Shortlisted/Accepted/Rejected/Withdrawn`.
- **Invented lifelines → drop them:** `AccessPolicy`, `StateMachinePolicy`, `ScholarshipStatusHistory`,
  `RespondToApplicationCommand`, `ReapplyCommand`, `SubmitScholarshipForAssessmentCommand`,
  `ScholarshipListing`, `DocSlot`/`VaultFile`. Real names: the **command/query handler** itself does the
  auth (`ICurrentUserService.IsInRole` / owner check) and the state guard is the static
  **`ApplicationStateMachine.EnsureTransition`**; the entity is **`Scholarship`** (not `ScholarshipListing`);
  status history is written by an **`ApplicationStatusChangedEvent`** MediatR handler, not a synchronous `Hist.record`.
- **HTTP status codes:** creates return **200 (new id)** not 201; submit/withdraw/save-draft/archive return
  **204** not 200; business-rule blocks throw `ConflictException` → **409** (not 422); only FluentValidation
  failures (empty/oversized/missing-field/blank-reason) are **422**.

### 6.2 Part 1 (Discovery/Listing) — key edits
- **State machine (SCH-ACT-16):** add the missing `Closed → UnderReview : owner/admin reopens (deadline re-checked)`;
  delete the fictional `Draft → In Assessment (submit)` (no submit-for-assessment command exists — provider
  create goes straight to `UnderReview`; a rejected `Draft` returns to `UnderReview` by *editing*).
- **Remove UC/ACT/SEQ-12** ("Submit Listing for Assessment") — no such command/endpoint.
- **SCH-SEQ-05 Apply-Now:** no `ScholarshipsController.ApplyNow` endpoint — it's client-driven (gate on
  `status != "Open"` then call the application-intent endpoint, or redirect to the external URL).
- **SCH-SEQ-01/02/03** collapse Browse/Search/Filter into the single `GetScholarshipsQuery` (`GET /api/scholarships`).
- **SCH-SEQ-13 approve:** controller is `ScholarshipsController` (not `AdminScholarshipsController`); add the
  `alt deadline within 7 days → 409` guard on approve. **SCH-SEQ-18 auto-close:** `ScholarshipAutoCloseJob`
  sets `Open→Closed` for `Deadline < now` (no controller/policy/history). **SCH-SEQ-11 archive:** `DELETE {id}` → 204 + soft-delete.
- **SCH-SEQ-04 detail:** `GetScholarshipById` returns 200 for any status; only 404 when the row is missing (no discovery-gate 404).
- **SCH-SEQ-06 bookmark:** pure toggle returning `bookmarked:true/false` — no action param, no duplicate branch, no Open-status guard.

### 6.3 Part 2 (Application submission/tracking) & part3 (responding/notifications/rating) — key edits
- **Documents (APP-SEQ-04):** upload goes through the **Documents** endpoint/command (not `ApplicationsController`);
  unsupported-type / MIME-mismatch / magic-byte-fail / infected → **409**; only empty/>25 MB → 422.
- **Submit (APP-SEQ-06):** completeness failures are **409** (not 422); success is **204**.
- **Reapply (APP-SEQ-09):** there is no `ReapplyCommand` — reapply is **`StartApplicationCommand`** (Withdrawn/Rejected/Accepted
  are excluded from the active-duplicate check, so Start re-creates a Draft). Start is idempotent → **200 (AlreadyExisted)**
  or **201 (new)**; it no longer 409s on an existing active application (resume).
- **Provider review (part3 SEQ-13..16):** ONE endpoint `POST /api/applications/{id}/review` `{Status, DecisionReason}` → **204**
  (no `respond`/`reject` endpoints). Reject-reason is enforced by `ReviewApplicationCommandValidator` → **422**. A provider can
  Shortlist/Accept/Reject directly from `Pending` (not only `UnderReview`).
- **Rating (part3 SEQ-19):** command is **`SubmitScholarshipProviderRatingCommand`** → `POST /api/company-reviews` → **200 {ReviewId}**.
  On save it **always** notifies the company (`ScholarshipProviderRatingReceived`); on a first low-rating dip it notifies **admins**
  (`ScholarshipProviderLowRatingFlagged`, 3-month window). It does **not** notify the student. Add a note: the **rated company is
  resolved server-side from the scholarship owner** (never client-supplied). Reconcile the FR id for the 3-month window (code = FR-APP-35, DESC-19 says FR-APP-31).
- **Submit notifications (part3 SEQ-18):** the submit handler dispatches **two** — `ApplicationSubmittedConfirmation` to the
  **student** (FR-APP-17) + `ApplicationSubmitted` to the company. UC/DESC-18 omit the student confirmation — add it.

## 7. Consultant Booking diagrams (v4) — documentation edits

Reviewed the 43 v4 diagrams against the CURRENT code (incl. the new PB-006R penalty
system). **The code is correct** (the penalty system was just built + shipped + audited);
these are SRS/PlantUML edits for you. No `System`-actor violations (the machine-initiated
UCs correctly use event-trigger actors: "Booking Status Change Event", "Booking Expiry
Trigger", "Session End Trigger", "Reminder Time Trigger", etc.).

### 7.1 Systemic (dominant root cause — fix globally across the write-path sequences)
- **HTTP status codes:** every booking mutation returns **`204 No Content`** (accept/reject/
  cancel/no-show/rate/availability), and every booking-rule failure throws `BookingDomainException`
  → **`422`** (not 409/403). Booking creation returns **`200`** (not 201). Fix all the
  `200/201/409/403` labels on SEQ-04/05/07/08/09/12/14 accordingly. (The admin-resolve's
  already-resolved 409 is the one real 409.)

### 7.2 Specific fixes
- **SEQ-13 Admin Validate No-Show:** endpoint is `POST /api/admin/no-show-reports/{id}/resolve`
  → **204** (not `/no-show/{id}/resolve` → 200). Command is `ResolveNoShowReportCommand(reportId,
  IsValid, AdminNote)` (boolean verdict). Add the missing branches: validated consultant no-show
  **also fully refunds the student** (diagram only shows −40%); false report sets the booking
  back to **`Completed`**; already-resolved → **409**.
- **SEQ-17 Reinstate Intake:** endpoint is `POST /api/admin/users/{userId}/reinstate-booking-intake`
  → **204**; the command takes no decision arg and only clears `BookingIntakeSuspendedAt`. Delete
  the fictional "keep restricted" else-branch.
- **SEQ-16 / ACT-14 low-rating:** the low-rating FLAG (`ConsultantRatingService` → sticky
  `ConsultantLowRatingFlaggedAt` + admin notify) does NOT stop new bookings. Intake suspension is a
  **separate** field `BookingIntakeSuspendedAt`, set by the rating-submit handler over the last-20
  window (min 5 samples) and checked in `RequestBooking`. Don't depict the flag as halting intake.
- **SEQ-04 Availability:** it's `PATCH /api/bookings/me/availability` on **`BookingsController`**
  → 204 (not `PUT /availability` on ConsultantsController → 200).
- **State machine ACT-05 (biggest gap):** use the code enum names (`NoShowStudent`/`NoShowConsultant`)
  and **add the new `NoShowReported` state**: `Confirmed → NoShowReported` (via report) and
  `NoShowReported → {NoShowStudent | NoShowConsultant | Completed}` (via admin resolve).

**Correct (no edit):** all 8 use-case diagrams; auto-expire (24h), reminders (24h+1h), complete,
and cancel-penalty flows (3-day block / −20%) match the code. ~27 of 43 clean.

---

### 6.4 ✅ Real code bug found & FIXED — withdrawal missing from the status timeline
`WithdrawApplicationCommandHandler` set `Status=Withdrawn` but (unlike Submit/Review) **never raised
`ApplicationStatusChangedEvent`**, so `ApplicationStatusHistoryEventHandler` never wrote a StatusHistory row —
a withdrawal never appeared in the student's timeline (FR-APP-18/19). Fixed: it now raises the event
(`statusBeforeWithdrawal → Withdrawn`); the payment-outcome handler only acts on Accepted/Rejected, so it's
side-effect-safe. Shipped separately.
