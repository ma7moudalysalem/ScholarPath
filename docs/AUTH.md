# Auth & Access

ScholarPath is a **gated platform**. Only the home page (`/`) is public; every other route requires an authenticated session.

## Roles

| Role         | Can                                                                                      |
|--------------|------------------------------------------------------------------------------------------|
| Guest        | View home page. Every protected request → 401 → redirect to `/login`.                     |
| Unassigned   | Logged in but not yet onboarded. Can only hit `/api/auth/onboarding/*`.                   |
| Student      | Discover, apply, track, book consultants, chat, use AI, read resources.                   |
| Company      | Create in-app listings, review applications, publish articles.                            |
| Consultant   | Manage availability, accept bookings, publish articles, earn via Stripe Connect.          |
| Admin        | Full oversight: approvals, moderation, financial config, audit.                           |

**Role switcher**: a user can hold both `Student` and `Consultant` roles; a JWT claim `active_role` controls the in-session view. `POST /api/auth/switch-role` re-issues tokens with a new `active_role`.

## Registration flow (email + password)

```
Guest                 Frontend                  API                       DB
  │  fill form           │                        │                        │
  │ ───────────────────▶│ POST /api/auth/register│                        │
  │                      │ ──────────────────────▶│                        │
  │                      │                        │ RegisterCommand        │
  │                      │                        │  • validate (FluentV)  │
  │                      │                        │  • hash PW (PBKDF2)    │
  │                      │                        │  • create user (Unass) │
  │                      │                        │  • issue tokens        │
  │                      │                        │ ──────────────────────▶│
  │                      │ ◀──────────────────────│ tokens + user          │
  │ ◀──────────────────│ set authStore          │                        │
  │ redirect /onboarding│                        │                        │
```

## Login (password)

Same shape, with additional rules:
- After 5 consecutive failed attempts inside 15 minutes → account locked 30 minutes (Identity `Lockout` settings in `Infrastructure/DependencyInjection.cs`).
- `rememberMe=true` extends the refresh-token lifetime from 7 days to 30 days.

## SSO (Google / Microsoft)

1. Frontend: `window.location = /api/auth/google/authorize?redirectUri=<return>`.
2. API redirects to provider with OpenID Connect `code` scope.
3. Provider → `/api/auth/google/callback?code=...&state=...`.
4. `SsoService` exchanges code → profile.
5. If email matches an existing user → link + issue tokens.
6. If new → create user in `Unassigned` state → tokens → `/onboarding`.

## Refresh rotation

Refresh tokens are **one-time-use with a replacement chain**. Every rotate:
- Marks the old token `IsRevoked = true`, stores `ReplacedByTokenId`.
- Issues a new refresh. Sending an already-used refresh revokes the whole chain (CSRF-hardening).

Storage: `RefreshToken` rows hashed with SHA-256. Raw values never logged.

## Onboarding

```
Unassigned user
  │
  ├─ chooses Student        →  activate immediately → /student
  ├─ chooses Company        →  AccountStatus = PendingApproval
  │                             → admin reviews in /admin/onboarding
  └─ chooses Consultant     →  AccountStatus = PendingApproval
                                → admin reviews in /admin/onboarding
```

Company/Consultant accounts can only access `/profile` and `/notifications` until approved.

## Upgrade Student → Consultant

`POST /api/users/upgrade-request` with files + links. Admin approves → user now holds both roles. Historical Student data is preserved.

## Gating enforcement — three layers

1. **Frontend route guards** — `RequireAuth` / `RequireRole` in `client/src/routes/RequireAuth.tsx`. Redirects to `/login?redirect=<from>`.
2. **Backend** — every controller except `AuthController` carries `[Authorize]` (global fallback policy in `Program.cs` will be added before production).
3. **SignalR** — hubs inherit `AuthenticatedHub` which carries `[Authorize]`. JWT passed via `access_token` query string (handled by `JwtBearerEvents.OnMessageReceived`).

## Password policy (FR-021)

- ≥8 characters
- ≥1 uppercase, ≥1 digit, ≥1 special character
- Hashed with PBKDF2 (ASP.NET Core Identity default)

Client-side Zod schema mirrors server rules for instant UX feedback.

## Security baseline

- JWT signed with **HMAC-SHA256** in scaffold (scratch key in `appsettings.json`). **Migrate to RS256 + Azure Key Vault before production** (`docs/deploy.md` checklist).
- TLS 1.2+ enforced in production (Kestrel config + Azure App Service default).
- Lockout policy (see above).
- Refresh token rotation with replacement chain.
- Brute-force defence: per-IP rate limiting on `/api/auth/*` (10/min).
- No passwords in logs or audit trails (Serilog destructuring config).
