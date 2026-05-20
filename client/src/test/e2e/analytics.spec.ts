// PB-015 T-017 — Analytics: route guards, smoke test for student/consultant/admin dashboards
import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

// --- Route guard tests ---

test("student/analytics redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/student/analytics");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("consultant/analytics redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/consultant/analytics");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

// --- Full flow smoke tests ---

test.describe("PB-015 T-017: analytics pages render without JS errors", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("student analytics page renders placeholder or embedded iframe", async ({ page }) => {
    // Collect JS errors during navigation
    const jsErrors: string[] = [];
    page.on("pageerror", (err) => jsErrors.push(err.message));

    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/student/analytics");

    // Either the Power BI iframe appears OR a not-yet-configured placeholder
    const content = page
      .frameLocator("iframe").locator("body").or(
        page.getByText(/analytics|not.*configured|coming soon|no data/i)
      ).or(
        page.getByRole("main")
      );
    await expect(content).toBeVisible({ timeout: 10_000 });

    // No unhandled JS errors
    expect(jsErrors.filter((e) => !e.includes("ResizeObserver"))).toHaveLength(0);
  });
});

test.describe("PB-015 T-017: consultant analytics smoke", () => {
  test.skip(!hasCreds("consultant"), "Requires E2E_CONSULTANT_EMAIL + E2E_CONSULTANT_PASSWORD env vars (seeded staging)");

  test("consultant analytics page renders placeholder or embedded iframe", async ({ page }) => {
    const jsErrors: string[] = [];
    page.on("pageerror", (err) => jsErrors.push(err.message));

    await loginAs(page, creds.consultant.email, creds.consultant.password);
    await page.goto("/consultant/analytics");

    const content = page
      .frameLocator("iframe").locator("body").or(
        page.getByText(/analytics|not.*configured|coming soon|no data/i)
      ).or(
        page.getByRole("main")
      );
    await expect(content).toBeVisible({ timeout: 10_000 });

    expect(jsErrors.filter((e) => !e.includes("ResizeObserver"))).toHaveLength(0);
  });
});

test.describe("PB-015 T-017: admin analytics smoke", () => {
  test.skip(!hasCreds("admin"), "Requires E2E_ADMIN_EMAIL + E2E_ADMIN_PASSWORD env vars (seeded staging)");

  test("admin analytics page renders without error", async ({ page }) => {
    const jsErrors: string[] = [];
    page.on("pageerror", (err) => jsErrors.push(err.message));

    await loginAs(page, creds.admin.email, creds.admin.password);
    await page.goto("/admin/analytics");

    // Should be on the admin analytics page — not redirect to /login
    await expect(page).not.toHaveURL(/\/login/, { timeout: 5_000 });

    const content = page
      .frameLocator("iframe").locator("body").or(
        page.getByText(/analytics|not.*configured|coming soon|no data/i)
      ).or(
        page.getByRole("main")
      );
    await expect(content).toBeVisible({ timeout: 10_000 });

    expect(jsErrors.filter((e) => !e.includes("ResizeObserver"))).toHaveLength(0);
  });
});
