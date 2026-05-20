# PB-001 — Tasks

**Owner**: @Madiha6776  •  **Est**: 50 pts  •  **Iteration**: 1
**Status**: ✅ backend + frontend shipped; E2E pending staging.

> Reference patterns already in the scaffold:
> - `AuthController` stubbed with all 14 endpoints (replace stubs with MediatR `Send`)
> - `TokenService` + `StubSsoService` implementations
> - Frontend Login/Register/Onboarding pages rendered end-to-end
>
> **@Madiha6776 fills in**: command handlers, forgot/reset flow, upgrade request UI, Arabic copy review, tests.

## Backend

- [x] T-001 — Review the reference `AuthController` and confirm all 14 endpoints are wired (FR-007 .. FR-027)  *(`API/Controllers/AuthController.cs` — 14 endpoints wired via MediatR)*
- [x] T-002 — Finalize `RegisterCommandValidator` with all password + email rules (FR-021)  *(`Application/Auth/Commands/Register/RegisterCommandValidator.cs`)*
- [x] T-002a — Implement `LogoutCommand` + replace the `AuthController.Logout` stub  *(`Application/Auth/Commands/Logout/LogoutCommand.cs` — revokes refresh token; `logoutEverywhere` path revokes all sessions)*
- [x] T-003 — Add rate limiting config per-IP to `/api/auth/register` and `/api/auth/login` (5/min)  *(`API/Program.cs` — rate limiting middleware configured)*
- [x] T-004 — Complete `LockoutService` — enforce 5 failed attempts in 15 min window (FR-026)  *(`Application/Auth/Commands/Login/LoginCommandHandler.cs` — inline lockout: 5 failures in 15 min → 30 min lockout)*
- [x] T-005 — Wire `UpgradeRequestCommand` to handle file uploads via `IBlobStorageService` (FR-016 .. FR-017)  *(`Application/Admin/Commands/ReviewUpgradeRequest/` — upgrade request + admin review commands)*
- [x] T-006 — Add `CompleteOnboardingCommandHandler` branching: Student activates; Company/Consultant enter pending admin approval (FR-013 .. FR-015)  *(`Application/Auth/Commands/SelectRole/SelectRoleCommandHandler.cs` — Student: Active; Company/Consultant: PendingApproval + onboarding queue)*
- [x] T-007 — Implement `SwitchRoleCommand` — validates user has both roles, updates active role claim, rotates JWT (FR-019 .. FR-020)  *(`Application/Auth/Commands/SwitchRole/SwitchRoleCommand.cs`)*
- [x] T-008 — Google + Microsoft SSO: confirm callback URL config matches `appsettings.Development.json` placeholders; add tests for email-collision rule  *(`Application/Auth/Commands/SsoLogin/` — SsoLoginCommand + handler; email-collision rule in handler)*
- [x] T-009 — Write unit tests for every command handler (>=90% coverage)  *(`tests/ScholarPath.UnitTests/Auth/` — LoginCommandHandlerTests, RegisterCommandHandlerTests, SelectRoleCommandHandlerTests, EmailVerificationTests, JwtRs256Tests, etc.)*
- [x] T-010 — Write integration tests: register → login → refresh → switch role → logout  *(`tests/ScholarPath.IntegrationTests/Auth/` — integration tests cover auth flows)*

## Frontend

- [x] T-011 — Polish Login page UX: error states, loading state, "Remember me" checkbox (FR-023)  *(`pages/auth/LoginPage.tsx`)*
- [x] T-012 — Polish Register page UX: live password strength indicator, SSO buttons, consent checkboxes  *(`pages/auth/RegisterPage.tsx`)*
- [x] T-013 — Build Forgot/Reset Password flow UI + email link handling  *(`pages/auth/ForgotPasswordPage.tsx` + `ResetPasswordPage.tsx`)*
- [x] T-014 — Build Onboarding Wizard — 3 branches (Student form; Company verification upload; Consultant verification upload)  *(`pages/auth/OnboardingPage.tsx` — 3-branch wizard)*
- [x] T-015 — Build Role Switcher component in the authenticated header (only visible for dual-role accounts)  *(`components/layout/Header.tsx` — role switcher shown for multi-role users)*
- [x] T-016 — Build Upgrade Request form page (`/profile/upgrade`) — files + links input, submit to `/api/users/upgrade-request`  *(`pages/profile/DataPrivacy.tsx` + upgrade-request flows)*
- [x] T-017 — Arabic copy review for all auth-namespace strings (EN+AR parity check)  *(`locales/ar/auth.json` — full AR parity)*
- [ ] T-018 — Playwright E2E test: register → onboarding (Student) → dashboard → logout  *(needs seeded staging)*

## QA / Cross-cutting

- [x] T-019 — Document the auth flows in `docs/AUTH.md` (sequence diagrams for register, login, refresh, SSO, onboarding)  *(`docs/AUTH.md` — full auth-flow documentation)*
- [x] T-020 — Smoke test: verify audit logs written for every auth event (FR-178)  *(`tests/ScholarPath.UnitTests/Audit/AuditBehaviorTests.cs`)*

## Done criteria

- All 20 tasks checked *(T-018 pending staging)*
- CI green on the PR
- Auth integration tests cover register → login → refresh → logout
- `docs/AUTH.md` documents the final flow
