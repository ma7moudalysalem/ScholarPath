# GDPR data-subject-rights audit (Task 5E)

Audit of the GDPR data-subject-rights (DSR) implementation — the right of
access (Art. 15, "export") and the right to erasure (Art. 17, "delete").

## Scope reviewed

- Jobs: `DataExportJob`, `DataDeleteJob` (`server/src/ScholarPath.Infrastructure/Jobs/`).
- Triggers: `RequestDataExportCommand`, `RequestDataDeleteCommand`,
  `CancelDataDeleteCommand`, `GetMyDataRequestsQuery`
  (`server/src/ScholarPath.Application/Audit/...`).
- API surface: `DataPrivacyController` (`/api/users/me/data-*`).
- The `UserDataRequest` entity and every PII-bearing related table
  (`User`/`UserProfile`, education, applications, bookings, reviews, forum
  posts/votes/flags, chat, AI interactions, notifications, payments, audit
  log, login attempts, refresh/reset tokens, success stories, saved
  scholarships, resource bookmarks/progress, upgrade requests).

## Findings and fixes

### 1. Export was grossly incomplete (Art. 15 violation) — FIXED

`DataExportJob.BuildExportPayloadAsync` exported only `User`, `Profile`,
`Applications`, `Bookings`, `Notifications`. It **omitted**: forum
posts/replies, forum votes, forum flags, chat messages, AI interactions
(prompt/response), company reviews, consultant reviews, education history,
saved scholarships, resource bookmarks/progress, upgrade requests, payments,
notification preferences. The exported `User`/`Application`/`Notification`
projections also dropped fields (`PhoneNumber`, `PersonalNotes`,
`FormDataJson`, notification body, etc.).

Fix: the payload now collects every related table keyed on the user, and the
projections include the personal fields. All queries use
`IgnoreQueryFilters()` so soft-deleted rows (still personal data the platform
holds) are included.

### 2. Erasure left PII in most related tables (Art. 17 violation) — FIXED

`DataDeleteJob` soft-deleted the `User` row, anonymised a handful of
`ApplicationUser` columns and four `UserProfile` columns, and revoked refresh
tokens. Everything else was untouched: chat message bodies, forum post
titles/bodies, AI prompt + response + metadata, review comments, education
institution/degree, application personal notes + submitted form JSON,
login-attempt email/IP/UA, audit-log IP/UA, refresh-token IP/UA, success-story
author name/image, saved-scholarship notes, booking meeting URLs. The user's
`PasswordHash` and security stamps also survived.

Fix: `AnonymiseUserAsync` now removes or irreversibly anonymises personal data
across every related table:
- **User**: clears `PasswordHash`, rotates `SecurityStamp`/`ConcurrencyStamp`,
  disables 2FA, clears phone/avatar/country/last-login, replaces name + email
  with anonymised placeholders.
- **Profile + education**: clears bio, socials, nationality, DOB, institution,
  org details, preference JSON; education rows institution/degree/field set to
  `[removed]`.
- **Chat / forum**: message bodies and post titles/bodies replaced with
  `[content removed]` and soft-deleted.
- **AI**: prompt, response, metadata cleared; redaction-audit samples cleared.
- **Reviews**: company/consultant review comments cleared.
- **Applications**: personal notes, form JSON, attachment JSON cleared.
- **Auth artefacts**: password-reset tokens deleted, refresh-token IP/UA
  stripped, login attempts' email/IP/UA stripped.
- **Audit log**: rows retained for accountability (Art. 5(2)) but `IpAddress`
  / `UserAgent` stripped.
- **Bookings**: personal meeting URLs cleared. **Success stories**: author
  name → "Former member", image cleared. **Saved scholarships**: notes cleared.

Records that must legally be retained — `Payment` / `Payout` — are
**kept**. They carry no name/email, only the user GUID, which now resolves to
the anonymised user row, so no PII remains linked.

### 3. `DeletedByUserId` mis-attributed — FIXED

The delete job set `user.DeletedByUserId = user.Id`, attributing the deletion
to the user being erased. The job runs without an actor, so this is now
`null` (system action). The same applies to the soft-delete of the user's
chat messages / forum posts.

### 4. Erasure / access events were not audited — FIXED

The DSR *request* commands are audited via the `[Auditable]` pipeline
behaviour, but the *jobs* that actually perform the export and erasure wrote
no `AuditLog` entry. Both jobs now append an `AuditLog` row
(`ActorUserId = null` system action) on completion — `Update` /
"Data export completed" for export, `Delete` / "Account erasure completed"
for delete.

### 5. Export against a soft-deleted user returned an empty user — FIXED

`db.Users` carries a global `!IsDeleted` query filter. If an export ran after
the account was deleted, the user lookup returned `null`. The export now uses
`IgnoreQueryFilters()` for the user lookup (both the payload and the
email-notification lookup).

## Items checked and found correct (no change needed)

- **Authorization.** `RequestDataExport`/`RequestDataDelete`/`CancelDataDelete`
  /`GetMyDataRequests` all resolve the subject from
  `ICurrentUserService.UserId` and throw `ForbiddenAccessException` when
  unauthenticated — a user can only act on their own data. The controller is
  `[Authorize]` and routed under `/api/users/me`. The jobs are background
  sweeps (no per-user authorization surface).
- **Idempotency / safety.** The request commands reject a second in-flight
  request with `409 Conflict`. Both jobs flip a request to `Processing` then
  `Completed`/`Failed`, so a re-run never reprocesses a finished request. The
  delete job's per-table anonymisation is itself idempotent (overwrites with
  the same placeholder / deletes again), and the missing-user case is handled
  gracefully — verified by `Delete_is_idempotent_when_rerun` and
  `Delete_handles_missing_user_and_still_completes_request`.
- **Cooling-off window.** The 30-day delete cooling-off and the cancel path
  are intact.

## Tests added

`server/tests/ScholarPath.UnitTests/Audit/`:

- `DataExportJobTests` (5 tests) — export includes personal data from every
  related table; request marked completed with download URL; audit-log entry
  written; completed requests skipped (idempotency); missing user handled.
- `DataDeleteJobTests` (6 tests) — user row anonymised (incl. password hash);
  PII anonymised across all related tables; request completed + audit-log
  entry written; cooling-off requests not processed; re-run is idempotent;
  missing user handled; financial payment records retained.

## Build / test result

- `dotnet build server/ScholarPath.slnx` → 0 errors, 0 warnings.
- `dotnet test server/tests/ScholarPath.UnitTests` → 239 passed, 0 failed
  (11 new).
