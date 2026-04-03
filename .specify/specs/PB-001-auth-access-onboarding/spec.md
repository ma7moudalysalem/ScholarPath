# PB-001 — Authentication, Access, and Onboarding

**Owner**: @Madiha6776 • **Priority**: Essential • **Iteration**: 1 • **Est**: 50 pts

## Problem statement

ScholarPath is a gated platform. Every route except `/` requires authentication. Guests must be able to register (email/password, Google SSO, Microsoft SSO), log in, reset passwords, and complete an onboarding flow that assigns them a role (Student, Company, Consultant). Company and Consultant registrations require admin approval; Student activates immediately. Students can later upgrade to Consultant and switch modes in-session.

## User stories (from SRS)

| ID | Story | Size |
|----|-------|------|
| US-001 | As a Guest, I can view the public home page so that I can understand the platform before registering. | 2pt |
| US-002 | As a Guest, I can register using email and password. | 3pt |
| US-003 | As a Guest, I can register or log in using Google. | 3pt |
| US-004 | As a Guest, I can register or log in using Microsoft. | 3pt |
| US-005 | As a User, I can log in using my credentials. | 3pt |
| US-006 | As a User, I can reset my password through an email link. | 4pt |
| US-007 | As an Unassigned user, I can complete onboarding to activate my role. | 4pt |
| US-008 | As an Unassigned user, I can choose Student to activate immediately. | 3pt |
| US-009 | As an Unassigned user, I can submit Company onboarding for admin review. | 5pt |
| US-010 | As an Unassigned user, I can submit Consultant onboarding for admin review. | 5pt |
| US-011 | As a Student, I can request an upgrade to Consultant. | 5pt |
| US-012 | As a Student-Consultant, I can switch active mode in session. | 5pt |
| US-013 | As the system, I can lock accounts after repeated failed logins. | 3pt |
| US-014 | As the system, I can block protected pages for unauthenticated users. | 2pt |

## Functional requirements covered

FR-001 .. FR-006 (access control), FR-007 .. FR-027 (auth + onboarding).

## Acceptance criteria

1. **Registration (email/password)** — POST `/api/auth/register` with `{email, password, firstName, lastName}` creates an `ApplicationUser` with `AccountStatus=Unassigned`, `IsOnboardingComplete=false`, issues access + refresh tokens, and redirects to `/onboarding`. Password rules: ≥8 chars, ≥1 uppercase, ≥1 digit, ≥1 special char.
2. **SSO** — `/api/auth/google/callback` and `/api/auth/microsoft/callback` handle the OAuth 2.0 code exchange, create-or-find a user by email, and route to `/onboarding` (new user) or `/dashboard` (existing user).
3. **Login** — POST `/api/auth/login` returns access token (60 min) + refresh token (7 days; 30 days with `rememberMe=true`). After 5 consecutive failed attempts in 15 minutes, account is locked for 30 minutes.
4. **Refresh** — POST `/api/auth/refresh` rotates refresh tokens with a replacement chain; reuse of a superseded token revokes the entire chain.
5. **Forgot/Reset password** — `/api/auth/forgot-password` emails a one-time link valid 1 hour; `/api/auth/reset-password` accepts the token + new password and invalidates all active refresh tokens for the user.
6. **Onboarding** — Student completes questionnaire and is activated immediately; Company/Consultant submit verification docs, receive `AccountStatus=PendingApproval`, and remain unable to access role-specific features until admin approves.
7. **Role switcher** — A user with both `Student` and `Consultant` roles sees an in-nav switcher that changes `activeRole` in their JWT (on next refresh) and reloads the dashboard.
8. **Upgrade request** — `/api/users/upgrade-request` with supporting files/links creates an `UpgradeRequest` row for admin review.
9. **Gated routes** — Any request to a non-public route without a valid JWT returns 401; frontend routes redirect to `/login?redirect=<original>`.
10. **Audit** — Every auth event (register, login, logout, lockout, SSO success/failure) produces an `AuditLog` row.

## Non-goals

- MFA (out of scope for v1)
- Password-less magic-link login (deferred)
- Social providers beyond Google + Microsoft
- LDAP / SAML enterprise auth

## Out-of-scope dependencies

- Email delivery (owned by PB-010 Notifications via `IEmailService`)
- Admin approval UI for Company/Consultant (PB-011)
- Profile data editing (PB-002)
