# PB-001 — Tasks

**Owner**: @Madiha6776  •  **Est**: 50 pts  •  **Iteration**: 1

> Reference patterns already in the scaffold:
> - `AuthController` stubbed with all 14 endpoints (replace stubs with MediatR `Send`)
> - `TokenService` + `StubSsoService` implementations
> - Frontend Login/Register/Onboarding pages rendered end-to-end
>
> **@Madiha6776 fills in**: command handlers, forgot/reset flow, upgrade request UI, Arabic copy review, tests.

## Backend

- [ ] T-001 — Review the reference `AuthController` and confirm all 14 endpoints are wired (FR-007 .. FR-027)
- [ ] T-002 — Finalize `RegisterCommandValidator` with all password + email rules (FR-021)
- [ ] T-003 — Add rate limiting config per-IP to `/api/auth/register` and `/api/auth/login` (5/min)
- [ ] T-004 — Complete `LockoutService` — enforce 5 failed attempts in 15 min window (FR-026)
- [ ] T-005 — Wire `UpgradeRequestCommand` to handle file uploads via `IBlobStorageService` (FR-016 .. FR-017)
- [ ] T-006 — Add `CompleteOnboardingCommandHandler` branching: Student activates; Company/Consultant enter pending admin approval (FR-013 .. FR-015)
- [ ] T-007 — Implement `SwitchRoleCommand` — validates user has both roles, updates active role claim, rotates JWT (FR-019 .. FR-020)
- [ ] T-008 — Google + Microsoft SSO: confirm callback URL config matches `appsettings.Development.json` placeholders; add tests for email-collision rule
- [ ] T-009 — Write unit tests for every command handler (>=90% coverage)
- [ ] T-010 — Write integration tests: register → login → refresh → switch role → logout

## Frontend

- [ ] T-011 — Polish Login page UX: error states, loading state, "Remember me" checkbox (FR-023)
- [ ] T-012 — Polish Register page UX: live password strength indicator, SSO buttons, consent checkboxes
- [ ] T-013 — Build Forgot/Reset Password flow UI + email link handling
- [ ] T-014 — Build Onboarding Wizard — 3 branches (Student form; Company verification upload; Consultant verification upload)
- [ ] T-015 — Build Role Switcher component in the authenticated header (only visible for dual-role accounts)
- [ ] T-016 — Build Upgrade Request form page (`/profile/upgrade`) — files + links input, submit to `/api/users/upgrade-request`
- [ ] T-017 — Arabic copy review for all auth-namespace strings (EN+AR parity check)
- [ ] T-018 — Playwright E2E test: register → onboarding (Student) → dashboard → logout

## QA / Cross-cutting

- [ ] T-019 — Document the auth flows in `docs/AUTH.md` (sequence diagrams for register, login, refresh, SSO, onboarding)
- [ ] T-020 — Smoke test: verify audit logs written for every auth event (FR-178)

## Done criteria

- All 20 tasks checked
- CI green on the PR
- Auth integration tests cover register → login → refresh → logout
- `docs/AUTH.md` documents the final flow
