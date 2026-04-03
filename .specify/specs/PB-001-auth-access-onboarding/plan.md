# PB-001 — Implementation Plan

## Architecture touchpoints

### Domain (`server/src/ScholarPath.Domain/`)
- **Entities**: `ApplicationUser`, `ApplicationRole`, `RefreshToken`, `LoginAttempt`, `UserProfile`, `UpgradeRequest`, `UpgradeRequestFile`, `UpgradeRequestLink`
- **Enums**: `AccountStatus { Unassigned, PendingApproval, Active, Suspended, Deactivated }`, `UpgradeTarget { Company, Consultant }`, `UpgradeRequestStatus { Pending, Approved, Rejected }`
- **Events**: `UserRegisteredEvent`, `UserLoggedInEvent`, `UserLockedOutEvent`, `UpgradeRequestSubmittedEvent`

### Application (`server/src/ScholarPath.Application/Auth/`)
- **Commands**: `RegisterCommand`, `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`, `SwitchRoleCommand`, `SubmitUpgradeRequestCommand`, `CompleteOnboardingCommand`
- **Queries**: `GetCurrentUserQuery`
- **SSO handlers**: `GoogleSsoCallbackCommand`, `MicrosoftSsoCallbackCommand`
- **Validators**: FluentValidation per command — email format, password rules, token presence

### Infrastructure (`server/src/ScholarPath.Infrastructure/`)
- `Services/TokenService.cs` — RS256 JWT issue + refresh rotation, replacement chain enforcement
- `Services/SsoService.cs` — Google + Microsoft code exchange, email-based account matching
- `Services/PasswordResetService.cs` — token generation + email dispatch
- `Persistence/Configurations/RefreshTokenConfiguration.cs` — unique index on hash, composite on `(UserId, IsRevoked)`
- `Identity/IdentitySeeder.cs` — seeds 5 roles: Admin, Student, Company, Consultant, Unassigned

### API (`server/src/ScholarPath.API/Controllers/AuthController.cs`)
Endpoints:
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET  /api/auth/me`
- `POST /api/auth/switch-role`
- `POST /api/auth/onboarding/student` (student activation)
- `POST /api/auth/onboarding/company` (with verification files)
- `POST /api/auth/onboarding/consultant` (with verification files)
- `POST /api/users/upgrade-request`
- `GET  /api/auth/google/authorize`
- `GET  /api/auth/google/callback`
- `GET  /api/auth/microsoft/authorize`
- `GET  /api/auth/microsoft/callback`

### Frontend (`client/src/`)
- **Pages**: `pages/auth/{Login, Register, ForgotPassword, ResetPassword, OnboardingWizard, SsoCallback}.tsx`
- **Components**: `components/auth/{SsoButtons, PasswordStrengthIndicator, OnboardingStepper}.tsx`
- **Layout**: `components/layout/PublicLayout.tsx` (home, login, register only)
- **Store**: `stores/authStore.ts` (Zustand 5) — holds user, tokens, active role
- **Guards**: `routes/RequireAuth.tsx`, `routes/RequireRole.tsx`
- **API client**: `services/api/auth.ts` — typed wrapper around `axios` instance with `/api/auth/*`

### i18n
Namespace `auth` in `locales/{en,ar}/auth.json` — covers all form labels, validation errors, CTA, success/error toasts.

### Tests
- Unit: each command handler (happy path + validation + edge cases like locked account, expired token)
- Integration: `AuthIntegrationTests` — full register→login→refresh→logout round trip via WebApplicationFactory + Testcontainers-SQL
- E2E: Playwright test in `client/src/test/e2e/auth.spec.ts` — register, login, forgot password, onboarding

## Dependencies

- None (foundational module; all other modules depend on this)

## Deliverables owned by this module

- Fully working auth flow (register, login, SSO, refresh, logout, forgot/reset)
- Role switcher component + backend endpoint
- Onboarding wizard (3 paths: Student/Company/Consultant)
- Upgrade request form + endpoint
- `AuthController` complete; used as reference pattern for every other controller
- Unit tests (≥90% coverage on handlers), integration tests, 1 E2E test

## Risks

1. **Token rotation replay** — must enforce replacement chain; unit test covers `reuse → chain revoked`
2. **SSO email collision** — existing email+password account tries SSO with same email → link accounts (config-gated)
3. **Onboarding incomplete state** — user with `IsOnboardingComplete=false` can only hit `/api/auth/onboarding/*` endpoints; all other API calls 403
4. **Concurrent refresh** — two devices refresh simultaneously → only one wins; the other must re-login (acceptable)
