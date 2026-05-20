# E2E Testing Guide

Playwright E2E tests live in `client/src/test/e2e/`.  
They cover route guards (always run) and full user flows (skip without credentials).

---

## Quick start — local dev

```bash
# 1. Start the backend (applies migrations + seeds demo data automatically)
cd server && dotnet run --project src/ScholarPath.API

# 2. In another terminal, copy the example env file
cd client && cp .env.e2e.example .env.e2e

# 3. Run E2E against localhost:5173 with the seeded demo credentials
npm run test:e2e:local
```

`npm run test:e2e:local` uses Node 22's `--env-file` flag to load `.env.e2e`  
and then runs `playwright test` (all projects: chromium, firefox, mobile).

---

## Run against staging

Set `E2E_BASE_URL` and the credential env vars, then run normally:

```bash
E2E_BASE_URL=https://scholarpath-staging.azurewebsites.net \
E2E_ADMIN_EMAIL=admin@scholarpath.local \
E2E_ADMIN_PASSWORD=Admin123! \
E2E_STUDENT_EMAIL=student@scholarpath.local \
E2E_STUDENT_PASSWORD=Student123! \
E2E_CONSULTANT_EMAIL=consultant@scholarpath.local \
E2E_CONSULTANT_PASSWORD=Consult123! \
E2E_COMPANY_EMAIL=company@scholarpath.local \
E2E_COMPANY_PASSWORD=Company123! \
npx playwright test
```

When `E2E_BASE_URL` is set the local `vite dev` server is **not** started —  
Playwright talks directly to the remote host.

---

## CI / GitHub Actions

The `.github/workflows/e2e.yml` workflow:

| Trigger | Description |
|---------|-------------|
| **Manual** (`workflow_dispatch`) | Run with an optional custom `base_url` input |
| Automatic | Uncomment the `workflow_run` block to auto-trigger after a deploy |

### Required repository secrets

Go to **Settings → Secrets and variables → Actions** and add:

| Secret | Value |
|--------|-------|
| `E2E_BASE_URL` | Staging deployment URL, e.g. `https://scholarpath-staging.azurewebsites.net` |
| `E2E_ADMIN_EMAIL` | `admin@scholarpath.local` |
| `E2E_ADMIN_PASSWORD` | `Admin123!` |
| `E2E_STUDENT_EMAIL` | `student@scholarpath.local` |
| `E2E_STUDENT_PASSWORD` | `Student123!` |
| `E2E_CONSULTANT_EMAIL` | `consultant@scholarpath.local` |
| `E2E_CONSULTANT_PASSWORD` | `Consult123!` |
| `E2E_COMPANY_EMAIL` | `company@scholarpath.local` |
| `E2E_COMPANY_PASSWORD` | `Company123!` |

> **Note**: The demo credentials above are the `DbSeeder` accounts created  
> on first app startup (see `server/src/ScholarPath.Infrastructure/Persistence/Seed/DbSeeder.cs`).  
> They must exist in the target environment before the E2E suite runs.

---

## Test structure

| File | Module | Always runs | Needs creds |
|------|--------|-------------|-------------|
| `auth.spec.ts` | PB-001 | Route guards (19) | Full sign-in/out flow |
| `profile.spec.ts` | PB-002 | `/profile` guard | Edit profile, change password |
| `scholarships.spec.ts` | PB-003 | Guard | Search + bookmark + company create |
| `applications.spec.ts` | PB-004 | Guard | Full apply→draft→submit→accept→withdraw lifecycle |
| `booking.spec.ts` | PB-006 | 3 guards | Happy-path Stripe, pre-accept cancel |
| `community.spec.ts` | PB-007 | 2 guards | Post/reply/vote/flag, chat+block |
| `resources.spec.ts` | PB-009 | 2 guards | Draft→submit→approve→publish |
| `notifications.spec.ts` | PB-010 | Guard | Bell-badge flow, preference opt-out |
| `payments.spec.ts` | PB-013 | Guard | Stripe booking+capture+receipt |
| `analytics.spec.ts` | PB-015 | Role guards | Dashboard smoke per role |
| `ai.spec.ts` | PB-008 | Guard | Recommendations, eligibility, chatbot |
| `admin.spec.ts` | PB-011 | Guard | Onboarding approval workflow |
| `mobile.spec.ts` | PB-015 T-018 | Login + overflow checks | Authenticated pages on Pixel 7 |

---

## Debugging failures

```bash
# Run a single spec with the Playwright UI (visual trace + time travel)
npm run test:e2e:ui -- --grep "applications"

# View the HTML report from the last run
npm run test:e2e:report

# Re-run only failed tests
npx playwright test --last-failed
```
