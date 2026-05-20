// PB-001 T-018 — Authentication: route guards, register page, login validation, full sign-in/sign-out flow
import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

const STUDENT_ROUTES = [
  "/student",
  "/student/scholarships",
  "/student/applications",
  "/student/bookmarks",
  "/student/consultants",
  "/student/bookings",
  "/student/community",
  "/student/resources",
  "/student/documents",
  "/student/ai",
  "/student/analytics",
];

const CONSULTANT_ROUTES = [
  "/consultant",
  "/consultant/availability",
  "/consultant/bookings",
  "/consultant/earnings",
  "/consultant/analytics",
];

const COMPANY_ROUTES = [
  "/company",
  "/company/scholarships",
  "/company/applications-review",
  "/company/billing",
];

for (const route of STUDENT_ROUTES) {
  test(`student route ${route} redirects unauthenticated user to /login`, async ({ page }) => {
    await page.goto(route);
    await page.waitForURL(/\/login/, { timeout: 5_000 });
    await expect(page).toHaveURL(/\/login/);
  });
}

for (const route of CONSULTANT_ROUTES) {
  test(`consultant route ${route} redirects unauthenticated user to /login`, async ({ page }) => {
    await page.goto(route);
    await page.waitForURL(/\/login/, { timeout: 5_000 });
    await expect(page).toHaveURL(/\/login/);
  });
}

for (const route of COMPANY_ROUTES) {
  test(`company route ${route} redirects unauthenticated user to /login`, async ({ page }) => {
    await page.goto(route);
    await page.waitForURL(/\/login/, { timeout: 5_000 });
    await expect(page).toHaveURL(/\/login/);
  });
}

test("register page renders with expected fields", async ({ page }) => {
  await page.goto("/register");
  await expect(page.getByLabel(/name/i).first()).toBeVisible({ timeout: 5_000 });
  await expect(page.getByLabel(/email/i)).toBeVisible();
  await expect(page.getByLabel(/password/i)).toBeVisible();
  // Role selector — could be a <select> or a set of radio/button options
  const roleSelector =
    page.getByRole("combobox", { name: /role/i }).or(
      page.getByRole("radiogroup", { name: /role/i })
    ).or(
      page.getByLabel(/role/i)
    );
  await expect(roleSelector).toBeVisible();
  await expect(page.getByRole("button", { name: /register|sign up|create account/i })).toBeVisible();
});

test("login form shows validation error on empty submit", async ({ page }) => {
  await page.goto("/login");
  await page.getByRole("button", { name: /sign in|login/i }).click();
  // Browser native validation or custom error message should fire
  const errorVisible =
    (await page.getByText(/required|invalid|enter your email/i).count()) > 0 ||
    (await page.locator(":invalid").count()) > 0;
  expect(errorVisible).toBe(true);
});

test.describe("full sign-in / sign-out flow (student)", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("student can log in and sign out", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await expect(page).toHaveURL(/\/student/, { timeout: 8_000 });

    // Sign out — look for a button or link labelled sign out / log out
    const signOutBtn = page.getByRole("button", { name: /sign out|log out|logout/i }).or(
      page.getByRole("link", { name: /sign out|log out|logout/i })
    );
    await signOutBtn.click();

    // Should land back on home or login
    await page.waitForURL((url) => url.pathname === "/" || url.pathname.startsWith("/login"), {
      timeout: 8_000,
    });
    await expect(page).not.toHaveURL(/\/student/);
  });
});
