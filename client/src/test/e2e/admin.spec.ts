import { test, expect } from "@playwright/test";

// PB-011 admin routes are guarded by RequireAuth + RequireRole([Admin, SuperAdmin]).
// Without a signed-in admin session, the guard should bounce the user to /login.
// These smoke tests run against a fresh dev server with no session state.

const ADMIN_ROUTES = [
  "/admin",
  "/admin/users",
  "/admin/onboarding",
  "/admin/upgrades",
  "/admin/analytics",
  "/admin/ai-economy",
  "/admin/redaction-audit",
  "/admin/broadcast",
  "/admin/audit-log",
];

for (const route of ADMIN_ROUTES) {
  test(`admin route ${route} redirects unauthenticated user to /login`, async ({ page }) => {
    await page.goto(route);
    // RequireAuth pushes to /login; give the router a tick to settle
    await page.waitForURL(/\/login/, { timeout: 5_000 });
    await expect(page).toHaveURL(/\/login/);
  });
}
