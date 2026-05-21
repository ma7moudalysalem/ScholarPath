# Code Review — Madiha's PB-001 / PB-002 / PB-010 Branches (orphan-history rescue + per-branch review)

**Reviewer**: @ma7moudalysalem · **Author**: @Madiha6776 · **Date**: 2026-04-25
**Branches reviewed**:
- `feat/PB-001-logout-command`
- `feat/PB-001-forgot-reset-password`
- `feat/PB-002-profile`
- `feat/PB-010-notifications`
- `test/PB-001-PB-002-unit-tests` *(skipped — content-identical to `feat/PB-002-profile`)*

**No PRs opened yet** — this review is the prerequisite for opening them.

---

## TL;DR

Mimi, two things to know up-front, in order:

1. **Your branches have no shared history with `main`.** That's why nothing shows in the diff and why no PR can open. The actual code is real and committed — git just can't see how it relates to main. This is a **mechanical git problem, not a code problem**, and the rescue takes ~30–60 minutes pairing. Plan in Part 1 below.
2. **Once we replay your commits onto main**, there are some real findings to address. The biggest are: (a) every new file you wrote landed in `ScholarPath/server/...` instead of `server/...` — a duplicated root folder that the build will not compile; (b) `ResetPasswordCommandHandler` brute-forces the reset token across every user in the database (security blocker); (c) most handlers are written against your own copies of `RefreshToken` / `UserProfile` / `Notification` rather than the ones on main, so they will not compile after replay until rewritten against main's entity shapes; (d) `IConfiguration` is injected in the Application layer (constitution principle II violation).

None of this is unrecoverable. The good code is still good — your handler logic for `ChangePassword`, `MarkAsRead`, `MarkAllAsRead`, and `GetNotifications` is solid. The fix is mostly mechanical: replay onto main, move files out of `ScholarPath/server/...`, and rewrite a handful of property references against main's entity shapes.

**Verdict**: do the rescue first, then iterate on the per-branch findings. We'll pair on the rescue.

---

# Part 1 — Git workflow rescue plan

## What happened (one paragraph)

Looking at all 5 of your branches, the commit history is internally consistent (linear, real commits), but `git merge-base main origin/feat/PB-001-logout-command` returns nothing. That means at some point the working copy was either (a) `git init`'d fresh from a downloaded ZIP of the repo instead of `git clone`'d, or (b) had its history rewritten in a way that severed the link to main (`git rebase --root`, `git filter-branch`, force-push from a fresh local repo, etc.). The code itself is fine — your commit graph just floats in space relative to main's graph, so `git diff main...branch` is empty and GitHub won't open a PR. **No data is lost** and **no work needs to be redone.** We just need to replay your commits on top of main.

The branch `feat/PB-010-notifications` is the most complete (it contains every commit on every other branch — 22 commits ending with `0ec41bc`), so we'll cherry-pick **from that one branch** and skip the rest.

## Step-by-step rescue (pairing, ~45 min)

### Step 0 — Make a local backup of her branches (1 min)
```bash
cd D:/Projects/ScholarPath
git fetch origin
git branch backup/madiha-pb010 origin/feat/PB-010-notifications
git branch backup/madiha-pb002 origin/feat/PB-002-profile
git branch backup/madiha-pb001-forgot origin/feat/PB-001-forgot-reset-password
git branch backup/madiha-pb001-logout origin/feat/PB-001-logout-command
```
*Why:* once we start cherry-picking, if anything goes wrong, we restore from these.

### Step 1 — Identify her commits (2 min)
The 7 commits that contain her actual work (the rest are inherited noise from a stale fork of main):
```
b292875 feat(auth): LogoutCommand + handler + client integration (PB-001 T-002a)
22049e5 feat(auth): ForgotPassword + ResetPassword commands (PB-001)
079c6cb feat auth ChangePassword PB-001
fa57c3d feat profile GetProfile query and DTO PB-002
1f8b2cc feat profile GetProfile UpdateProfile commands PB-002
7b2d3ea feat profile UploadProfileImage and complete PB-002
ba99ade test unit tests for Logout Password and Profile PB-001 PB-002
0ec41bc feat notifications GetNotifications MarkAsRead MarkAllAsRead PB-010
```

### Step 2 — Create one rescue branch per epic, off main (2 min)
```bash
git checkout main
git pull origin main

git checkout -b rescue/PB-001-auth-madiha
git checkout main && git checkout -b rescue/PB-002-profile-madiha
git checkout main && git checkout -b rescue/PB-010-notifications-madiha
```

### Step 3 — Cherry-pick PB-001 commits onto `rescue/PB-001-auth-madiha` (5–10 min)
```bash
git checkout rescue/PB-001-auth-madiha
git cherry-pick b292875 22049e5 079c6cb
```
**Expect conflicts**, mostly on `ScholarPath/server/...` paths (see Step 4 — these need to be moved). Resolve each conflict by:
- Accepting her **content** but moving it to the correct path `server/...` (NOT `ScholarPath/server/...`).
- For controllers (`AuthController.cs`), **merge by hand** — main already has a stub controller with `Logout`/`ForgotPassword`/`ResetPassword` endpoints. Replace each `NotImplementedForTeam(...)` line with a real `Mediator.Send(...)` call.

### Step 4 — Move her files to the correct path
She accidentally created a nested `ScholarPath/server/...` folder. After cherry-pick, on each branch run:
```bash
# Move every file from the wrong path to the correct path
git mv ScholarPath/server/src/ScholarPath.Application/Auth/Commands/Logout \
       server/src/ScholarPath.Application/Auth/Commands/Logout
# … repeat for ForgotPassword, ResetPassword, ChangePassword, Profile, Notifications, controllers, tests …
# When done, the nested ScholarPath/ folder should be empty:
rm -rf ScholarPath
git add -A && git commit -m "chore(rescue): move files from ScholarPath/server/ to server/"
```

### Step 5 — Reconcile entity shapes (15 min — this is the careful part)
Her code was written against her own `RefreshToken`, `UserProfile`, `Notification` classes. Main's versions are different:

| Her code uses | Main has |
|---|---|
| `RefreshToken.Token` (plaintext) | `RefreshToken.TokenHash` (hashed) |
| `RefreshToken.RevokedReason` | `RefreshToken.RevokedReason` ✓ same |
| `UserProfile.GPA` | `UserProfile.Gpa` (lower-case `pa`) |
| `UserProfile.Bio` | `UserProfile.Biography` |
| `UserProfile.Country` | `UserProfile.Nationality` (close enough) |
| `Notification.Title` / `.TitleAr` / `.Message` / `.MessageAr` | `Notification.TitleEn` / `.TitleAr` / `.BodyEn` / `.BodyAr` |
| `Notification.UserId` | `Notification.RecipientUserId` |
| `Notification.RelatedEntityId` / `RelatedEntityType` | `Notification.MetadataJson` (or add as new fields) |
| `IEmailService` in `Domain.Interfaces` | n/a — does not exist on main; introduce in `Application.Common.Interfaces` |

For each handler, do a search-and-replace inside the file. Run `dotnet build` after each branch and fix what's red.

### Step 6 — Wire the controllers (5 min)
Main's `AuthController` is currently a scaffold. Replace each `NotImplementedForTeam(...)` body with the corresponding `Mediator.Send(...)` call. Pattern:
```csharp
[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout(CancellationToken ct)
{
    await _mediator.Send(new LogoutCommand(GetRefreshTokenCookie(), logoutEverywhere: false), ct);
    Response.Cookies.Delete("RefreshToken");
    return NoContent();
}
```

### Step 7 — Push rescue branches & open PRs (2 min)
```bash
git push -u origin rescue/PB-001-auth-madiha
git push -u origin rescue/PB-002-profile-madiha
git push -u origin rescue/PB-010-notifications-madiha
gh pr create --base main --head rescue/PB-001-auth-madiha --title "feat(auth): PB-001 — logout / forgot / reset / change password (Madiha)"
# repeat for PB-002 and PB-010
```

### Step 8 — Delete the orphan branches once merged
After the rescue branches are merged, the old orphan branches `feat/PB-001-logout-command`, `feat/PB-001-forgot-reset-password`, `feat/PB-002-profile`, `feat/PB-010-notifications`, and `test/PB-001-PB-002-unit-tests` can be deleted on the remote.

### Step 9 — Going forward: clone, don't init
```bash
# Don't do this:
mkdir ScholarPath && cd ScholarPath && git init  # ← this severs history

# Do this instead:
git clone https://github.com/<org>/ScholarPath.git
cd ScholarPath
git checkout -b feat/PB-xxx-my-thing main   # ← this preserves history
```

**Total estimate**: 30–60 min pairing session, ~75% of which is the entity-shape reconciliation in Step 5. Block out an hour and we'll do it together.

---

# Part 2 — Per-branch code review

> **Note on file paths in the links below**: every path is shown as it appears on her branch (`ScholarPath/server/...`). After the rescue these will be at `server/...`.

---

## Branch 1 — `feat/PB-001-logout-command`

**Last commit**: `b292875` (2026-04-19) · **Spec**: PB-001 T-002a
**Files**: `LogoutCommand.cs`, `LogoutCommandHandler.cs` (+ inherited noise commits)

### 🔴 Blockers

#### B1. Files live in the wrong path (`ScholarPath/server/...` instead of `server/...`)
[`ScholarPath/server/src/ScholarPath.Application/Auth/Commands/Logout/LogoutCommand.cs`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/Logout/LogoutCommand.cs)

Both files are nested inside an extra `ScholarPath/` folder. The project root is already `D:/Projects/ScholarPath/` — you've doubled it. The build will not pick these files up. Fix during the rescue (Part 1, Step 4).

#### B2. `LogoutCommandHandler` queries `RefreshToken.Token` (plaintext) but main stores `TokenHash`
[`ScholarPath/server/src/ScholarPath.Application/Auth/Commands/Logout/LogoutCommandHandler.cs:18`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/Logout/LogoutCommandHandler.cs:18)

```csharp
.FirstOrDefaultAsync(t => t.Token == request.RefreshToken && !t.IsRevoked, ct);
```

Main's [`RefreshToken`](server/src/ScholarPath.Domain/Entities/Identity.cs:56) intentionally stores the **SHA256 hash** of the token (a security baseline — constitution principle VI), not the token itself. Querying `t.Token == ...` will fail to compile against main's entity shape, and even if it did compile it would never match anything.

**Fix (post-rescue):**
```csharp
var tokenHash = TokenHasher.Sha256(request.RefreshToken);
var refreshToken = await _dbContext.RefreshTokens
    .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsRevoked, ct);
```

#### B3. `LogoutEverywhere=true` should call `IUserAdministration.RevokeAllSessionsAsync`, not roll its own loop
[`ScholarPath/server/src/ScholarPath.Application/Auth/Commands/Logout/LogoutCommandHandler.cs:25-37`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/Logout/LogoutCommandHandler.cs:25)

The PB-001 task description (T-002a) explicitly says: _"call `IUserAdministration.RevokeAllSessionsAsync` (already shipped in PB-011) to kill every active session."_ That service is at [`server/src/ScholarPath.Application/Common/Interfaces/IUserAdministration.cs:22`](server/src/ScholarPath.Application/Common/Interfaces/IUserAdministration.cs:22) and exists for exactly this reason. Use it instead of duplicating the loop.

```csharp
if (request.LogoutEverywhere)
    await _userAdministration.RevokeAllSessionsAsync(refreshToken.UserId, "User logout (everywhere)", ct);
```

### 🟡 Important

#### I1. Missing `[Auditable]`
PB-001 task T-002a literally says _"Mark the command `[Auditable(AuditAction.Logout, "User")]`"._ Add it on top of the command class. The audit pipeline picks it up automatically (constitution principle VII).

#### I2. The current `AuthController` on her branch implements logout inline (bypasses the command)
On `feat/PB-010-notifications`, [`server/src/ScholarPath.API/Controllers/AuthController.cs:145`](server/src/ScholarPath.API/Controllers/AuthController.cs:145) has a hand-rolled `Logout` action that talks to the DbContext directly — and the new `LogoutCommand` is never wired in. Pick one path; the command-handler one is the correct one (Clean Architecture, principle II).

#### I3. `DateTime.UtcNow` instead of `DateTimeOffset.UtcNow`
The rest of the codebase uses `DateTimeOffset` for audit timestamps (look at any other handler for examples). Use the same.

### 🟢 Nice-to-have

#### N1. The handler returns `Unit.Value` even when no token is found
That's the right behavior (idempotent logout), but log a warning so we can spot abuse if someone is repeatedly hitting `/logout` with garbage tokens.

### ✅ What's good

- **The shape of the command is correct**: a plain `IRequest<Unit>` with the two relevant inputs (`RefreshToken`, `LogoutEverywhere`).
- **Empty-not-found returning `Unit.Value`** is good — logout should never reveal whether a token is valid.
- **Setting `RevokedReason`** is helpful for forensic analysis later.

---

## Branch 2 — `feat/PB-001-forgot-reset-password`

**Last commit**: `fa57c3d` (2026-04-20) · **Spec**: PB-001 acceptance criteria #5 + FR-025
**Adds**: `ForgotPasswordCommand`, `ResetPasswordCommand`, `ChangePasswordCommand` + handlers, validators, DTOs

### 🔴 Blockers

#### B1. `ResetPasswordCommandHandler` brute-forces the token across **every user**
[`ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ResetPassword/ResetPasswordCommandHandler.cs:23-37`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ResetPassword/ResetPasswordCommandHandler.cs:23)

```csharp
foreach (var user in await _userManager.Users.ToListAsync(ct))
{
    var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
    if (result.Succeeded) { targetUser = user; break; }
}
```

This loads every user in the database into memory, then loops trying to reset their password with the supplied token. It is:
1. **Catastrophic at scale** — O(N) PBKDF2 verifications per request.
2. **A timing oracle** — an attacker can measure how long the request takes and infer database size growth.
3. **Wrong** — the reset token can collide for users with the same token salt; the wrong account could be reset.

**Fix:** the `ResetPasswordCommand` must include the **email** (or user id) along with the token, so we resolve the user up front:
```csharp
public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Unit>;

var user = await _userManager.FindByEmailAsync(request.Email)
    ?? throw new DomainException("errors.auth.invalidResetToken");

var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
if (!result.Succeeded) throw new DomainException("errors.auth.invalidResetToken");
```

The frontend already gets the email back from the `/forgot-password` link — it's literally in the email URL she generates ([`ForgotPasswordCommandHandler.cs:36`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs:36)) — just thread it through to the API.

#### B2. `IConfiguration` injected into the Application layer
[`ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs:13`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs:13)

The Application layer is not allowed to depend on `Microsoft.Extensions.Configuration` (constitution principle II — Clean Architecture; same finding as Tasneem's PB-006 review item I2). Replace with `IOptions<AppSettings>`:

```csharp
public class AppSettings { public string ClientUrl { get; set; } = "http://localhost:3000"; }

// In Infrastructure DI:
services.Configure<AppSettings>(config.GetSection("App"));

// In the handler:
private readonly AppSettings _settings;
public ForgotPasswordCommandHandler(..., IOptions<AppSettings> options) => _settings = options.Value;
// usage: _settings.ClientUrl
```

#### B3. No expiry / one-time / rate limit enforcement on the reset link (spec acceptance criterion #5)
The spec is explicit: _"emails a one-time link valid 1 hour"._ Identity's default `DataProtectorTokenProvider` lifespan is 24 hours, not 1 hour. There's also no enforcement that the same token can only be used **once** before being invalidated (Identity tokens are stateless, so a successful `ResetPasswordAsync` does NOT auto-invalidate the token). The `/forgot-password` endpoint also has no rate limit — a script can flood any email address.

**Fix:**
- Configure `DataProtectionTokenProviderOptions.TokenLifespan = TimeSpan.FromHours(1);` in `Program.cs`.
- Increment `ApplicationUser.SecurityStamp` after a successful reset (this auto-invalidates any other in-flight reset tokens for that user).
- Add `[EnableRateLimiting("auth")]` on the `ForgotPassword` controller endpoint (the policy is already defined on main).

#### B4. `ResetPasswordRequestValidator` exists but is never invoked
The validator file is at [`server/src/ScholarPath.Application/Auth/Validators/ResetPasswordRequestValidator.cs`](server/src/ScholarPath.Application/Auth/Validators/ResetPasswordRequestValidator.cs) — but the command handler never calls it, and the controller doesn't either. Either wire the validator via a `ValidationBehavior` MediatR pipeline (preferred — already exists for other commands), or call it explicitly in the controller before sending.

### 🟡 Important

#### I1. Missing `[Auditable]` on all 3 commands
`ForgotPasswordCommand`, `ResetPasswordCommand`, `ChangePasswordCommand` are all state-mutating auth events that the spec requires to be audited (acceptance criterion #10 — _"Every auth event ... produces an `AuditLog` row"_).

```csharp
[Auditable(AuditAction.PasswordResetRequested, "User")]
public record ForgotPasswordCommand(string Email) : IRequest<Unit>;
```
*(Add `PasswordResetRequested`, `PasswordResetCompleted`, `PasswordChanged` to `AuditAction` if not already there.)*

#### I2. `ResetPassword` doesn't check that ALL active refresh tokens are revoked **per user** correctly
[`ResetPasswordCommandHandler.cs:39-49`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ResetPassword/ResetPasswordCommandHandler.cs:39)

The intent is correct (revoke all tokens — FR-025 ✓), but because `targetUser` is resolved by brute force, by the time you reach this code the user object may not be the actual owner of the reset token. Once B1 is fixed this becomes correct.

#### I3. `InvalidOperationException` instead of a domain exception
Same finding as Tasneem's PB-006 I5: throwing `InvalidOperationException` causes the global exception handler to return 500. Introduce `AuthDomainException` (or use the existing pattern) so the middleware can map to 400/422.

#### I4. `ChangePassword` revokes refresh tokens but doesn't issue a new pair
[`ChangePasswordCommandHandler.cs`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ChangePassword/ChangePasswordCommandHandler.cs)

The user is currently logged in, changes their password successfully, you revoke all their tokens — and then return `Unit.Value`. The user's next request will fail with 401 because their access token is still valid (60 min) but their refresh token is gone. Two options:
- Return the new `(AccessToken, RefreshToken)` pair from the handler so the controller can set new cookies.
- Or document that the client MUST log the user out on a successful change-password (the current behavior). Pick one and write a test.

### 🟢 Nice-to-have

#### N1. Email enumeration timing attack still possible
[`ForgotPasswordCommandHandler.cs:30-33`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs:30)

You correctly return `Unit.Value` early when the user is not found (good — prevents disclosure via response body). But the timing differs: a missing user returns ~5ms; a real user takes ~150ms (Identity hashing + email send). Add a `Task.Delay(Random.Shared.Next(80, 200), ct)` in the not-found branch, or run the email send fire-and-forget so timing is uniform.

#### N2. URL-encoded twice?
[`ForgotPasswordCommandHandler.cs:36`](ScholarPath/server/src/ScholarPath.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs:36)

`Uri.EscapeDataString(token)` is correct, but `_userManager.GeneratePasswordResetTokenAsync` returns a token that may already contain `+` and `/` which `EscapeDataString` will turn into `%2B` and `%2F`. The `/reset-password` page must decode them before sending back. Add an integration test that round-trips the URL.

### ✅ What's good

- **`ChangePasswordCommandHandler` is well-structured** — checks current user, verifies old password via `_userManager.ChangePasswordAsync` (correct API), revokes refresh tokens. Logic is right; just needs the Auditable + token-rotation fix.
- **`ForgotPasswordCommandHandler` returns `Unit.Value` for unknown emails** — correct security posture (no enumeration via response body).
- **Validators are thorough**: password rules match spec exactly (`≥8 chars, uppercase, lowercase, digit, special, ≤256, ≠ current`).
- **`ConfirmNewPassword` validation in the request DTO** — saves a round-trip if the user mistypes.
- **Reset notification side-effect (revoking refresh tokens)** is the right thing to do per FR-025.

---

## Branch 3 — `feat/PB-002-profile`

**Last commit**: `ba99ade` (2026-04-20) · **Spec**: PB-002 (Profile)
**Adds**: `GetProfileQuery`, `UpdateProfileCommand`, `UploadProfileImageCommand`, `UserProfileDto`, `ProfileController`, `UserProfile` entity, `MappingProfile` changes

### 🔴 Blockers

#### B1. `GetProfileQuery` has no handler — only the query record exists
[`ScholarPath/server/src/ScholarPath.Application/Profile/Queries/GetProfileQuery.cs`](ScholarPath/server/src/ScholarPath.Application/Profile/Queries/GetProfileQuery.cs)

There's a `GetProfileQuery` record but no `GetProfileQueryHandler.cs` anywhere on this branch. The controller endpoint `[HttpGet] GetProfile` will throw a MediatR runtime error (`No handler registered for ...`). Add the handler (it's basically the inverse of `UpdateProfileCommandHandler` — read user + profile, project to DTO, return).

#### B2. `UploadProfileImage` writes to `wwwroot/uploads/profiles/` on local disk
[`ScholarPath/server/src/ScholarPath.Application/Profile/Commands/UpdateProfile/UploadProfileImage/UploadProfileImageCommandHandler.cs:34-43`](ScholarPath/server/src/ScholarPath.Application/Profile/Commands/UpdateProfile/UploadProfileImage/UploadProfileImageCommandHandler.cs:34)

Three problems stacked here:
1. **The Application layer must not touch the filesystem** (Clean Architecture). Use `IBlobStorageService` (the interface already exists on main and is wired to local-disk in dev / Azure Blob in prod).
2. **No size or content-type validation** — anyone can upload a 4 GB binary or an `application/x-msdownload` file disguised as `.jpg`. Spec PB-002 T-004: _"size/type validation"_. Constitution VI: _"File uploads antivirus-scanned before persistence."_
3. **Path traversal** — `Path.GetExtension(request.FileName)` is taken directly from user input. A filename like `evil.jpg/../../web.config` will compute the wrong extension; worse, the saved path is `Path.Combine(uploadsFolder, fileName)` where `fileName` includes a user-controlled extension. Sanitize it.

**Fix:** delegate to `IBlobStorageService.UploadAsync(stream, contentType, sizeBytes, ct)`, validate extension is in `[".jpg", ".jpeg", ".png", ".webp"]`, validate `request.FileStream.Length <= 5_000_000`, validate magic bytes (not just content-type).

#### B3. Folder path & namespace mismatch on `UploadProfileImage`
- Folder: `Profile/Commands/UpdateProfile/UploadProfileImage/`
- Namespace: `ScholarPath.Application.Profile.Commands.UploadProfileImage` (no `UpdateProfile`)

Same finding as Tasneem's B2. Folder must equal namespace. Move the folder up one level: `Profile/Commands/UploadProfileImage/`.

#### B4. `UserProfile` entity collides with main's `UserProfile`
[`server/src/ScholarPath.Domain/Entities/UserProfile.cs`](server/src/ScholarPath.Domain/Entities/UserProfile.cs) (her branch) vs [`server/src/ScholarPath.Domain/Entities/Identity.cs:82`](server/src/ScholarPath.Domain/Entities/Identity.cs:82) (main).

Main already has a `UserProfile` class inside `Identity.cs` with significantly more fields (`Biography`, `DateOfBirth` as `DateOnly`, `Nationality`, `LinkedInUrl`, `WebsiteUrl`, `Timezone`, `AcademicLevel`, `Gpa`, `GpaScale`, `PreferredCountriesJson`, ...). Hers has `Bio`, `GPA`, `Country`, `Interests` (bare strings, no JSON). Two profiles can't live in the same namespace.

**Fix during rescue:** delete her `UserProfile.cs` entirely. In `UpdateProfileCommandHandler.cs`, change every `profile.Bio` → `profile.Biography`, `profile.GPA` → `profile.Gpa`, `profile.Country` → `profile.Nationality`, etc. Drop fields that don't exist on main (`TargetCountry`, `Interests`) or add them to main as a separate, scoped PR.

### 🟡 Important

#### I1. Missing `[Auditable]` on `UpdateProfileCommand` and `UploadProfileImageCommand`
Spec PB-002 references FR-177 (audit). Both commands mutate user state.

#### I2. `UpdateProfileCommandHandler` calls `_userManager.UpdateAsync(user)` without checking the result
[`UpdateProfileCommandHandler.cs:38`](ScholarPath/server/src/ScholarPath.Application/Profile/Commands/UpdateProfile/UpdateProfileCommandHandler.cs:38)

`UserManager.UpdateAsync` returns `IdentityResult` — if it fails (concurrency conflict, validation rule), you silently swallow the error. Check `.Succeeded` and throw on failure.

#### I3. No `ProfileUpdatedEvent` raised
Spec PB-002 T-007: _"Raise `ProfileUpdatedEvent` when relevant AI-input fields change (feeds PB-008)."_ Without this, the AI recommendation engine doesn't know to recompute. Add a domain event when `FieldOfStudy`, `Gpa`, or `PreferredCountries` changes.

#### I4. Frontend (`Profile.tsx`, `Security.tsx`, `UpgradeAccount.tsx`) was added but no tests
Vitest tests for these components are missing. Constitution V: ≥70% coverage.

### 🟢 Nice-to-have

#### N1. `CalculateCompleteness` is a static method on the handler — extract it
[`UpdateProfileCommandHandler.cs:81`](ScholarPath/server/src/ScholarPath.Application/Profile/Commands/UpdateProfile/UpdateProfileCommandHandler.cs:81)

Spec T-003 calls for a `ProfileCompletenessCalculator` ("unit-test heavy"). Extract it to a service so `GetProfileQueryHandler` (when you write it — see B1) can reuse the same logic and so the unit tests don't need to spin up a handler.

#### N2. Magic numbers in completeness calculation
`total += 3` then `total += 6` — name them. `const int UserFields = 3;` etc.

#### N3. `decimal? GPA` validation
No FluentValidation on `UpdateProfileCommand` to bound GPA (a typo could submit `9999.9`). Add `When(x => x.GPA.HasValue).RuleFor(x => x.GPA!.Value).InclusiveBetween(0m, 4m)` (or whatever scale you use).

#### N4. `UpdateProfileCommand` has no validator at all
Compare to `ChangePasswordCommand` which does. Add `UpdateProfileCommandValidator`.

### ✅ What's good

- **`UpdateProfileCommandHandler`'s "create-or-update" pattern** (find profile, if null create + add to DbSet, then assign fields) is correct EF Core idiom.
- **The completeness calculation logic itself is sensible** — counts filled fields out of total, returns a percentage. Easy to unit test.
- **`UserProfileDto` is a clean record** — immutable, no navigation properties, no entity leakage.
- **`ProfileController` is thin** (good — controllers should be pass-throughs per Clean Architecture).
- **Per-field nullable update** (`if (request.FirstName is not null) user.FirstName = request.FirstName;`) correctly supports PATCH semantics.

---

## Branch 4 — `feat/PB-010-notifications`

**Last commit**: `0ec41bc` (2026-04-20) · **Spec**: PB-010 T-004
**Adds**: `GetNotificationsQuery`, `MarkAsReadCommand`, `MarkAllAsReadCommand`, `NotificationDto`, `NotificationController`, `Notification` entity, `NotificationType` enum

### 🔴 Blockers

#### B1. `Notification` entity collides with main's `Notification`
[`server/src/ScholarPath.Domain/Entities/Notification.cs`](server/src/ScholarPath.Domain/Entities/Notification.cs) (her branch) vs [`server/src/ScholarPath.Domain/Entities/Notifications.cs`](server/src/ScholarPath.Domain/Entities/Notifications.cs) (main).

Main has a far more complete shape: `RecipientUserId` (not `UserId`), `TitleEn` + `TitleAr`, `BodyEn` + `BodyAr`, `Channel`, `Priority`, `IdempotencyKey`, `DispatchedAt`, `DispatchSucceeded`, soft-delete fields. Plus a `NotificationPreference` sibling entity.

**Fix during rescue:** delete her `Notification.cs` and `NotificationType.cs`. Rewrite the handlers/DTO against main's shape:

```csharp
// MarkAsReadCommandHandler — change UserId → RecipientUserId
if (notification.RecipientUserId != userId) throw new ForbiddenException(...);

// NotificationDto — change Title/Message → TitleEn/BodyEn (or take both in one DTO)
public record NotificationDto(Guid Id, NotificationType Type, string TitleEn, string TitleAr,
    string BodyEn, string BodyAr, bool IsRead, DateTimeOffset? ReadAt, ...);
```

#### B2. `MarkAsReadCommandHandler` is not idempotent
[`ScholarPath/server/src/ScholarPath.Application/Notifications/Commands/MarkAsReadCommand.cs:31-32`](ScholarPath/server/src/ScholarPath.Application/Notifications/Commands/MarkAsReadCommand.cs:31)

```csharp
notification.IsRead = true;
notification.ReadAt = DateTime.UtcNow;
```

If the notification is **already read**, the handler still writes `ReadAt = now`, overwriting the original read timestamp. That breaks reporting ("when did the user actually first read this?"). Make it a no-op:

```csharp
if (notification.IsRead) return Unit.Value;
notification.IsRead = true;
notification.ReadAt = DateTimeOffset.UtcNow;
```

#### B3. `OrderByDescending(n => n.Id)` instead of `CreatedAt`
[`GetNotificationsQueryHandler.cs:32`](ScholarPath/server/src/ScholarPath.Application/Notifications/Queries/GetNotificationsQueryHandler.cs:32)

`Notification.Id` is a `Guid`, not a sequential int. Sorting GUIDs lexicographically produces an essentially random order — the user will see notifications in the wrong sequence. Sort by `n.CreatedAt DESC` instead.

### 🟡 Important

#### I1. Missing `[Auditable]` on the two write commands
`MarkAsReadCommand` and `MarkAllAsReadCommand` mutate state. Add the attribute.

#### I2. Multiple classes per file
[`MarkAsReadCommand.cs`](ScholarPath/server/src/ScholarPath.Application/Notifications/Commands/MarkAsReadCommand.cs)

The file contains `MarkAsReadCommand` AND `MarkAsReadCommandHandler`. Style on this codebase is one class per file (look at any other Commands folder). Same problem in `MarkAllAsReadCommand.cs`. Split them into `MarkAsReadCommandHandler.cs` etc.

#### I3. Sub-namespaces vs folder layout
[`Notifications/Commands/`](ScholarPath/server/src/ScholarPath.Application/Notifications/Commands) has no per-feature folders. Compare to PB-001 (`Auth/Commands/Login/`, `Auth/Commands/Register/`). Restructure to `Notifications/Commands/MarkAsRead/MarkAsReadCommand.cs` for consistency.

#### I4. No total-unread-count endpoint
The spec implies a notification bell with an unread count badge (T-008). The current API forces the client to fetch all notifications and count `!IsRead` client-side. Add `GET /api/notifications/unread-count` for cheap polling.

#### I5. `MarkAllAsRead` loads every unread row into memory
[`MarkAllAsReadCommand.cs:24-32`](ScholarPath/server/src/ScholarPath.Application/Notifications/Commands/MarkAllAsReadCommand.cs:24)

`.ToListAsync()` then `foreach { ... }` then `SaveChangesAsync` — for a power user with 5,000 unread notifications this is 5,001 round-trips' worth of change tracking. EF Core 7+ supports bulk update:

```csharp
await _dbContext.Notifications
    .Where(n => n.RecipientUserId == userId && !n.IsRead)
    .ExecuteUpdateAsync(s => s
        .SetProperty(n => n.IsRead, true)
        .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow), ct);
```

### 🟢 Nice-to-have

#### N1. `PaginatedNotificationResponse` should be generic
The codebase already has a `PaginatedList<T>` somewhere — reuse it instead of inventing a notification-specific shape.

#### N2. Page size has no upper bound
[`GetNotificationsQuery.cs:7`](ScholarPath/server/src/ScholarPath.Application/Notifications/Queries/GetNotificationsQuery.cs:7)

`pageSize = 20` default but a malicious client can request `pageSize=1000000`. Cap it: `Math.Min(request.PageSize, 100)`.

#### N3. No SignalR push
Spec T-002 / T-012: real-time delivery via `NotificationHub`. The current handlers only persist to DB — the bell will not light up until the user refreshes. Out of scope for this branch, but track as a TODO.

### ✅ What's good

- **`NotificationController` is thin and well-named.**
- **Authorization check in `MarkAsReadCommandHandler`** (`if (notification.UserId != userId) throw`) is exactly right — only the recipient can mark their own.
- **`MarkAllAsReadCommandHandler` correctly scopes to the current user** (spec PB-010 acceptance: only own notifications).
- **Pagination is implemented** (Skip/Take with page+pageSize) — basic but correct.
- **DTOs are records** — immutable, easy to map.

---

## Cross-cutting findings (apply to all 4 branches)

These are listed once here instead of repeated per branch:

1. **Folder-name = namespace mismatch**: every command on her branches lives in `ScholarPath/server/...` but the C# namespace inside the file is `ScholarPath.Application....` (no leading `ScholarPath` from the duplicated folder). After the rescue (Step 4) this resolves automatically.
2. **`[Auditable]` missing on all 9 state-mutating commands** (Logout, ForgotPassword, ResetPassword, ChangePassword, UpdateProfile, UploadProfileImage, MarkAsRead, MarkAllAsRead — PB-001 spec acceptance #10 + constitution VII).
3. **Tests are tautological** ([`PasswordCommandsTests.cs`](ScholarPath/server/tests/ScholarPath.UnitTests/PasswordCommandsTests.cs), [`ProfileQueryTests.cs`](ScholarPath/server/tests/ScholarPath.UnitTests/ProfileQueryTests.cs)). Each test is `var c = new XCommand(...); Assert.Equal(arg, c.Arg);` — that proves the C# compiler works, not that your handler does. Need real handler tests with NSubstitute mocks for `_userManager`, `_dbContext`. Spec PB-001 T-009 says **≥90% coverage**; current effective coverage is ~0%.
4. **`InvalidOperationException` thrown for business-rule violations** in 3 places — should be a domain exception that the middleware maps to 400/422. Same as Tasneem's I5.
5. **`DateTime.UtcNow` vs `DateTimeOffset.UtcNow`** — main standardized on `DateTimeOffset`. Search-and-replace once during the rescue.
6. **No frontend tests** for any of the new pages (`Profile.tsx`, `Security.tsx`, `Notifications.tsx`, `ForgotPassword.tsx`, `ResetPassword.tsx`).

---

## Spec coverage summary

| Spec | Tasks | Done in her branches | Notes |
|---|---|---|---|
| PB-001 (Auth) | 20 | **~5** (T-002a Logout, T-013 ForgotPassword UI, change-password command, partial T-009 tests) | T-003 rate-limit, T-004 lockout, T-006 onboarding branching, T-007 SwitchRole, T-008 SSO callbacks not done. |
| PB-002 (Profile) | 18 | **~6** (T-002 Update, T-004 UploadPhoto*, T-005 ChangePassword, T-009 Profile.tsx, T-011 Security.tsx, partial T-008 tests) | T-001 GetProfile handler missing (only query exists). T-003 ProfileCompletenessCalculator not extracted. T-007 ProfileUpdatedEvent missing. T-012/T-013/T-014 (Consultant/Company/Public profile pages) not done. |
| PB-010 (Notifications) | 15 | **~3** (T-004 MarkRead/MarkAllRead, query, T-008 Notifications.tsx) | T-001 NotificationPreference not done. T-002 Dispatcher service not done. T-003 domain-event subscribers not done. T-005 preferences API not done. T-006 IEmailService MailKit/SendGrid not done. T-011 BroadcastComposer not done. T-012 SignalR client not done. |

**Total tasks delivered (estimated): ~14 of 53 across her three epics.** The unit-tests branch ostensibly covers PB-001 T-009 + PB-002 T-008 but, as noted in cross-cutting #3, the tests don't really test the handlers.

---

## Pre-merge checklist (post-rescue, per epic)

### `rescue/PB-001-auth-madiha`
- [ ] Files moved out of `ScholarPath/server/...` to `server/...`
- [ ] B2 — `LogoutCommandHandler` queries `TokenHash` not `Token`
- [ ] B3 — `LogoutEverywhere` uses `IUserAdministration.RevokeAllSessionsAsync`
- [ ] PB-001-FRP B1 — `ResetPasswordCommand` includes email; no brute-force loop
- [ ] PB-001-FRP B2 — `IConfiguration` replaced with `IOptions<AppSettings>`
- [ ] PB-001-FRP B3 — Reset link expiry 1h + SecurityStamp invalidation + rate limit
- [ ] PB-001-FRP B4 — `ResetPasswordRequestValidator` wired (via ValidationBehavior)
- [ ] `[Auditable]` on Logout / ForgotPassword / ResetPassword / ChangePassword
- [ ] AuthController endpoints rewired to `Mediator.Send`
- [ ] Real handler tests (>=80% coverage on these 4 handlers)

### `rescue/PB-002-profile-madiha`
- [ ] B1 — `GetProfileQueryHandler.cs` created
- [ ] B2 — `UploadProfileImageCommandHandler` uses `IBlobStorageService` + size + content-type validation
- [ ] B3 — Folder rename `UpdateProfile/UploadProfileImage` → `Profile/Commands/UploadProfileImage`
- [ ] B4 — Her duplicate `UserProfile` entity deleted; handlers rewritten against main's `UserProfile`
- [ ] `UpdateProfileCommandValidator` added with GPA bounds
- [ ] `ProfileCompletenessCalculator` extracted to a service (T-003)
- [ ] `ProfileUpdatedEvent` raised when AI-input fields change (T-007)

### `rescue/PB-010-notifications-madiha`
- [ ] B1 — Her duplicate `Notification` entity deleted; handlers rewritten against main's `Notification` shape (`RecipientUserId`, `TitleEn`, `BodyEn`, ...)
- [ ] B2 — `MarkAsReadCommandHandler` is idempotent
- [ ] B3 — `GetNotifications` orders by `CreatedAt DESC`
- [ ] `[Auditable]` on `MarkAsRead` / `MarkAllAsRead`
- [ ] `MarkAllAsRead` uses `ExecuteUpdateAsync` (bulk)
- [ ] `GET /api/notifications/unread-count` endpoint added
- [ ] `pageSize` capped at 100

---

## ✅ Things I enjoyed reviewing

- **`ChangePasswordCommandHandler`** — verifies current password via `_userManager.ChangePasswordAsync` (right API), revokes refresh tokens (FR-025), uses `ICurrentUserService` (no impersonation). Clean.
- **`ForgotPasswordCommandHandler` returns `Unit.Value` for unknown emails** — correct security posture against enumeration.
- **`MarkAsReadCommandHandler`'s authorization check** (`if (notification.UserId != userId) throw new UnauthorizedAccessException`) is exactly right.
- **Validators are thorough**: password complexity, ConfirmNewPassword, ≤256 char limits — matches the spec word-for-word.
- **Records everywhere for commands and DTOs** — immutable, no setters, no surprises.
- **Profile completeness calc** — simple, deterministic, easy to test once extracted.
- **Frontend pages were also added** (`Profile.tsx`, `Security.tsx`, `Notifications.tsx`, `ForgotPassword.tsx`, `ResetPassword.tsx`) even though that wasn't strictly required for the backend slice — shows initiative.

The structural problems are all replayable / mechanical. The decision-making (validation rules, what to revoke, who can mark what as read) is mostly right. Let's pair on the rescue and we'll get this to a mergeable state quickly. 

— Mahmoud
