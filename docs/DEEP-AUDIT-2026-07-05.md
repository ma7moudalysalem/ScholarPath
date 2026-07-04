# Deep System Audit — 2026-07-05 (business + code)

6-agent parallel deep review (payments, consultant booking/penalties, applications/scholarships,
auth/RBAC/data-protection, data-integrity/concurrency/jobs/perf, frontend). Each finding was
verified against the actual code; confidence noted where relevant. The system is broadly solid
and well-hardened — these are the real residual problems, prioritized by impact.

---

## 🔴 Critical — money, privacy/security, or data integrity

### C1. Provider can read students' **unsubmitted Draft** applications (privacy / IDOR-class) — VERIFIED
`GetScholarshipProviderApplicationsQueryHandler.cs:22` + `GetScholarshipProviderApplicationDetailsQueryHandler`
filter only by scholarship ownership and never exclude `Status == Draft`. A `Draft` row is created the
instant a student clicks "Apply" (before filling/paying) and keeps receiving `FormDataJson`/`AttachedDocumentsJson`.
So the owning provider can list + open the in-progress form answers and uploaded documents of applications
the student has **not submitted and may never submit**.
**Fix:** add `a.Status != ApplicationStatus.Draft` (or `a.SubmittedAt != null`) to both provider queries.

### C2. ScholarshipProviderReview fee can be **double-partial-refunded** (Withdraw + Cancel paths race)
The one unified `Payment` row is mutated by two disjoint subsystems (request-lifecycle vs application-event/withdraw).
`RefundScholarshipProviderReviewCommand.cs:98-100` computes a partial refund as `AmountCents / 2` **ignoring the
prior `RefundedAmountCents`**, with a different Stripe idempotency key than the request-cancel path. Odd-cents case
(e.g. 999¢: 500¢ then 499¢) passes the `>AmountCents` guard → **two Stripe refunds totalling the full charge** on a
payment meant to be half-refunded.
**Fix:** compute partial as `(AmountCents/2) - RefundedAmountCents`; refuse if already `PartiallyRefunded`; ideally
route all ScholarshipProviderReview money through one authority/idempotency key.

### C3. ScholarshipProviderReview money can **strand** — `Payment.RelatedApplicationId` is an optional client value
`StartScholarshipProviderReviewRequestCommand.cs:270` sets `RelatedApplicationId = request.ApplicationTrackerId` (nullable —
Apply-Now can run before the tracker exists). Every non-request-lifecycle money op (`CaptureScholarshipProviderReviewPayment`,
`RejectScholarshipProviderReviewPayment`, `RefundScholarshipProviderReview`, `WithdrawApplication`, `ScholarshipProviderReviewTimeoutRefundJob`)
finds the payment by `RelatedApplicationId == app.Id`. If it was null/mismatched, the **14-day timeout refund never
fires** and the app-Rejected event never cancels the hold → the student's money sits Held/Captured forever
(`IntegrityCheckJob` already counts these as orphan payments).
**Fix:** make the Payment↔tracker link mandatory + server-derived, or drive all ScholarshipProviderReview money through the request lifecycle.

### C4. No-show resolution (my PB-006R code) is **not atomic + penalty not idempotent + refund doesn't handle uncaptured/partial**
`ResolveNoShowReportCommand.cs`: (a) it commits the report as resolved, then applies the rating deduction in a
**separate** save — a failure between them leaves the report `Validated` with **no penalty applied** (and a manual
retry is blocked by the `PendingReview` guard, so the penalty is permanently lost). `ApplyPenaltyFactorAsync`
has no "already applied for this event" marker. (b) `RefundStudentAsync` refunds full `AmountCents` without
checking `Payment.Status == Captured` (a `Held`/authorized intent → Stripe 400 → the report can never be resolved)
and without netting `RefundedAmountCents`.
**Fix:** wrap resolution (state + penalty + refund) in one transaction; stamp the report with an applied-penalty
marker for idempotency; mirror `CancelBookingCommandHandler`'s captured-vs-authorized refund branching + net prior refunds.

### C5. Notification idempotency is check-then-act with a **non-unique** index → duplicate notifications/emails
`NotificationDispatcher.cs:29-33` does `if (!AnyAsync(key)) insert`; the `IdempotencyKey` index (`EntityConfigurations.cs:958`)
is not unique. Two concurrent dispatches with the same key (overlapping job runs, sweep + manual mark, webhook re-delivery)
both pass and both insert. The "exactly once" the whole job layer relies on is not enforced.
**Fix:** `HasIndex(n => n.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL")` + catch 2601/2627 as a no-op.

### C6. `ChangeUserRole` has **no privilege-tier guard** — an Admin can self-escalate to SuperAdmin
`ChangeUserRoleCommandHandler.cs` (gated only by controller `[Authorize(Roles="Admin,SuperAdmin")]`) lets an
`active_role == Admin` account `POST /admin/users/{ownId}/roles {Role:"SuperAdmin", Add}` and vertically escalate,
or grant Admin/SuperAdmin to anyone. No higher-tier-only or no-self-target rule.
**Fix:** require `active_role == "SuperAdmin"` to add/remove Admin or SuperAdmin; forbid changing one's own roles.

---

## 🟠 High — correctness / consistency

### H1. Country filter is an unbounded substring match (same class as the fixed field-of-study `&` bug)
`GetScholarships` country filter uses raw `.Contains(request.Country)` → `"oman"` matches `["Romania"]`, `"United"`
matches UK/US/UAE. **Fix:** mirror the field-of-study fix — `.Contains(JsonSerializer.Serialize(country.Trim()))`.

### H2. `UpdateScholarship` has no deadline re-check
Create/Approve/Reopen enforce the 7-day rule; `UpdateScholarshipCommand` sets `Deadline` with no validation → an
admin-owned Open listing can be edited to a past deadline and stay Open. **Fix:** add the rule to the update validator.

### H3. `PaymentCapturedEvent` is **never raised** → PB-018 analytics stream is dead
Defined + a stream handler consumes it, but `grep "new PaymentCapturedEvent"` = 0. The webhook flips to `Captured`
without raising it. Revenue analytics from that stream are incomplete. **Fix:** raise it from `payment_intent.succeeded`.

### H4. `ScholarshipProviderReviewTimeoutRefundJob` — per-iteration Stripe refunds, single terminal `SaveChanges` (+ N+1)
If the process dies mid-loop, Stripe refunded N payments but **zero** DB rows persisted → reprocessed next run.
**Fix:** `SaveChanges` per application inside the loop; batch-load payments (one query, not per-app `FirstOrDefault`).

### H5. `ConsultantLowRatingFlaggedAt` is set but **never cleared** anywhere
No command sets it back to null (`ReinstateBookingIntake` clears only `BookingIntakeSuspendedAt`). The flag is
permanent, and because re-notify requires it to be null, admins are alerted **once ever** — a recovered-then-dropped
consultant never re-alerts. **Fix:** add an admin clear (or clear on reinstate / on recovery above threshold).

### H6. Two low-rating mechanisms use inconsistent inputs
Admin flag = penalized lifetime average, min 1 review; intake-suspend = raw last-20 average, min 5. They routinely
disagree, and the flag can fire on a single noisy review. **Fix:** align both on the same basis + minimum sample.

### H7. SuperAdmin RBAC seam — many handlers `IsInRole("Admin")` without `SuperAdmin`; refresh drops role claims
Because `RoleClaimType = "active_role"`, a session acting as SuperAdmin fails handlers that check only `"Admin"`
(GetPayment, RefundPayment, Approve/RejectScholarship, all Resources admin cmds, GetFlaggedPosts, DismissPostFlags,
Hide*Review) → spurious 403 (safe-failing, but broken for a SuperAdmin operator). Separately, `TokenService.RotateRefreshTokenAsync`
reissues with an empty role list, so after a refresh `DeletePostCommand.cs:29` (`currentUser.Roles.Contains("Admin")`)
fails even for a legit Admin. **Fix:** standardise on an `IsAdminOrSuperAdmin()` helper everywhere; carry roles through the refresh rotation.

### H8. `CompletionJob` can auto-complete a one-party-joined booking before the sweep files a no-show
If the no-show sweep lags > 6h (deploy/outage), a one-sided-join booking crosses the 6h line and `CompletionJob`
marks it `Completed` (full payment retained), permanently closing the no-show remedy. **Fix:** CompletionJob should
skip/route one-party-joined bookings (require both-or-neither joined) or the sweep must run before completion age.

### H9. Dead `ScholarshipProviderReviewPayment` table + its webhook branch (two payment models for one concept)
`ProcessStripeWebhookCommand.ApplyToScholarshipProviderReviewPaymentAsync` operates on a legacy table the active flow no longer
writes. **Fix:** delete the entity + branch (confirm no seed/migration writes it).

---

## 🟡 Medium / low

- **Submit doc validation is positional parallel-array** (`SubmitApplicationCommand.cs:64-75`) — a client sending
  documents in a different order can pass the wrong doc for a required slot. Use a keyed `{docType: fileName}` map.
- **Chat SignalR stale-token closure** (`lib/signalr.ts:11`) — auto-reconnect re-auths with the captured (old) token
  after rotation; use the live-store reader like `services/signalR/hubs.ts`.
- **Soft-deleted `NoShowReport` blocks re-report** — unique index `UX_NoShowReports_Booking_Accused` isn't filtered on
  `IsDeleted`; add `.HasFilter("[IsDeleted] = 0")` (latent — no delete path today).
- **No-show refund notification** reuses `payment-refund:{id}` key → a 2nd refund on a payment sends no notice.
- **PII encryption scope** — `OrganizationTaxNumber`/`OrganizationRegistrationNumber`/`ContactPhoneNumber` are plaintext-at-rest
  (TDE only); extend the AES-GCM converter if defence-in-depth is intended.
- **Redaction sampling job** scans `AiInteractions` with no `(Feature, StartedAt)` index (fine at current volume).
- **Admin-notify fan-out** — O(admins) round-trips + 2 saves each per alert; batch into one `AddRange`+save.
- **Frontend polish:** `text-error-600` is a non-existent token (asterisks render uncolored) → `text-danger-500`;
  self-vote button on own root post not disabled (server-rejected → toast); free-path Submit button lacks a
  double-click guard (409 toast); dead `ApplyConfirmation.tsx` has a hardcoded English disclaimer (delete);
  no unsaved-changes navigation guard on draft/profile forms; `formatUsd` hardcodes `$`+Western digits.

---

## Verified sound (checked, no action) — so they're not re-audited
Refund idempotency keys + rounding (remainder-to-payee, no leak); free-mode skips Stripe cleanly; webhook
`StripeEventId` unique + re-query; `StripePayoutJob` pre-claim + RowVersion + DisableConcurrentExecution;
`FlagPostCommand` RowVersion retry loop; soft-delete query filters + GDPR `IgnoreQueryFilters`; domain events
dispatch after commit; all filtered unique indexes match their predicates; refresh reuse-detection (revoke-family);
SSO external-link-first + signed state; login lockout 5/15→30; UpdateProfile mass-assignment defence; IDOR ownership
on documents/recordings/bookings/payments/chat + ChatHub participant gate; prod-safe exception handler; RS256-only JWT;
booking-overlap SERIALIZABLE re-check + unique slot index; client crash-guards + mutation onError coverage + Stripe
double-charge guards.
