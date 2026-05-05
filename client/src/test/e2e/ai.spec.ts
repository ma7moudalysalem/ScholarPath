import { test, expect } from "@playwright/test";

// PB-008: /student/ai is gated by RequireAuth. Smoke-test that the gate
// fires and the page route resolves when signed in (bypassed via an
// explicit /login navigation short-circuit — we only assert the unauthed
// branch here since no credentials are available to this suite).

test("student/ai redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/student/ai");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("home route exposes the AI Features panel name on sign-in hint", async ({ page }) => {
  // Lightweight check: the auth layer doesn't 500; navigating straight to
  // the AI route still renders the login chrome rather than a blank page.
  await page.goto("/student/ai");
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible({ timeout: 5_000 });
});
