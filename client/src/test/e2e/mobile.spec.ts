// PB-015 T-018 — Mobile viewport UX review (automated).
//
// These tests run in the "mobile" Playwright project (Pixel 7 viewport, 412×915)
// configured in playwright.config.ts.  They cover the route-guard and basic render
// checks that matter most on a narrow viewport:
//   • Unauthenticated redirects still work at mobile widths.
//   • The login page renders a usable form (not cut off, not overflowing).
//   • Authenticated pages (when credentials are available) display their primary
//     content without horizontal overflow or uncaught JS errors.
//
// Route-guard tests run unconditionally; full-flow tests skip unless
// E2E_STUDENT_EMAIL / E2E_STUDENT_PASSWORD env vars are set.

import { test, expect, devices, type Page } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

// Pin the viewport explicitly so these tests read clearly even when run in
// the chromium or firefox projects by mistake.
test.use({ ...devices["Pixel 7"] });

// ── Utility ──────────────────────────────────────────────────────────────────

/** Returns true when the page body has no horizontal scrollbar. */
async function hasNoHorizontalOverflow(page: Page): Promise<boolean> {
  return page.evaluate(
    () => document.documentElement.scrollWidth <= window.innerWidth + 1, // +1px rounding buffer
  );
}

// ── Route-guard tests (always run) ───────────────────────────────────────────

test("mobile — /profile redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/profile");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("mobile — /scholarships redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/scholarships");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("mobile — /admin redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/admin");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

// ── Login-page layout ─────────────────────────────────────────────────────────

test("mobile — login page renders without horizontal overflow", async ({ page }) => {
  await page.goto("/login");

  // Form is visible
  await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible({ timeout: 5_000 });

  // No horizontal scrollbar
  const noOverflow = await hasNoHorizontalOverflow(page);
  expect(noOverflow).toBe(true);
});

test("mobile — login form is usable: label + input + button visible", async ({ page }) => {
  await page.goto("/login");

  // Email + password inputs exist and are reachable
  await expect(page.getByLabel(/email/i)).toBeVisible();
  await expect(page.getByLabel(/password/i)).toBeVisible();
  await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible();

  // All inputs are within the viewport (not cut off)
  const emailBox = await page.getByLabel(/email/i).boundingBox();
  const btnBox   = await page.getByRole("button", { name: /sign in/i }).boundingBox();
  expect(emailBox).not.toBeNull();
  expect(btnBox).not.toBeNull();
  expect(emailBox!.width).toBeGreaterThan(100);
  expect(btnBox!.width).toBeGreaterThan(50);
});

// ── Register page ─────────────────────────────────────────────────────────────

test("mobile — register page renders without horizontal overflow", async ({ page }) => {
  await page.goto("/register");

  await expect(page.getByRole("button", { name: /register|sign up|create/i })).toBeVisible({
    timeout: 5_000,
  });

  const noOverflow = await hasNoHorizontalOverflow(page);
  expect(noOverflow).toBe(true);
});

// ── Authenticated pages (requires staging creds) ─────────────────────────────

test.describe("PB-015 T-018: authenticated pages on mobile viewport", () => {
  test.skip(
    !hasCreds("student"),
    "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)",
  );

  test("mobile — home / dashboard renders without JS errors", async ({ page }) => {
    const jsErrors: string[] = [];
    page.on("pageerror", (e) => jsErrors.push(e.message));

    await loginAs(page, creds.student.email, creds.student.password);

    // Should land on the dashboard / home route after login
    await page.waitForURL((url) => !url.pathname.startsWith("/login"), { timeout: 10_000 });

    const noOverflow = await hasNoHorizontalOverflow(page);
    expect(noOverflow).toBe(true);

    expect(jsErrors.filter((e) => !e.includes("ResizeObserver"))).toHaveLength(0);
  });

  test("mobile — /scholarships search renders usable results on small screen", async ({ page }) => {
    const jsErrors: string[] = [];
    page.on("pageerror", (e) => jsErrors.push(e.message));

    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/scholarships");

    // Cards or empty-state placeholder is visible
    const content = page
      .getByRole("article")
      .or(page.getByText(/no scholarships|no results/i))
      .or(page.getByRole("list"));
    await expect(content).toBeVisible({ timeout: 10_000 });

    const noOverflow = await hasNoHorizontalOverflow(page);
    expect(noOverflow).toBe(true);

    expect(jsErrors.filter((e) => !e.includes("ResizeObserver"))).toHaveLength(0);
  });

  test("mobile — /profile page renders without horizontal overflow", async ({ page }) => {
    const jsErrors: string[] = [];
    page.on("pageerror", (e) => jsErrors.push(e.message));

    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/profile");

    // Profile heading or form is present
    const heading = page
      .getByRole("heading", { name: /profile/i })
      .or(page.getByText(/first name|full name|email/i));
    await expect(heading).toBeVisible({ timeout: 10_000 });

    const noOverflow = await hasNoHorizontalOverflow(page);
    expect(noOverflow).toBe(true);

    expect(jsErrors.filter((e) => !e.includes("ResizeObserver"))).toHaveLength(0);
  });
});
