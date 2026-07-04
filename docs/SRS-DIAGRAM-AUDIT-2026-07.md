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
