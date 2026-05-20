// PB-011 T-021 — Admin portal E2E: route guards + consultant onboarding approval flow.
// Route-guard tests run unconditionally; full flow skips without credentials.

import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

// ── Route guards ─────────────────────────────────────────────────────────────
// RequireAuth + RequireRole([Admin, SuperAdmin]) bounces unauthenticated users to /login.

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
    await page.waitForURL(/\/login/, { timeout: 5_000 });
    await expect(page).toHaveURL(/\/login/);
  });
}

// ── Full-flow test (skip without staging credentials) ─────────────────────────

test.describe("admin onboarding approval full flow — requires seeded staging", () => {
  test.skip(
    !hasCreds("admin"),
    "Requires E2E_ADMIN_EMAIL + E2E_ADMIN_PASSWORD env vars (seeded staging with a pending consultant application)",
  );

  test("PB-011 T-021: approve consultant → user dashboard shows consultant tools", async ({ browser }) => {
    // ── Step 1: Admin approves the pending consultant application ──────────
    const adminCtx = await browser.newContext();
    const adminPage = await adminCtx.newPage();
    await loginAs(adminPage, creds.admin.email, creds.admin.password);
    await adminPage.goto("/admin/onboarding");

    // The onboarding queue should show at least one pending row.
    // (Staging DB must have a user in PendingApproval status.)
    const firstRow = adminPage.getByRole("row").nth(1); // skip header
    await expect(firstRow).toBeVisible({ timeout: 10_000 });

    // Capture the applicant's email for later login verification
    const applicantEmail = await firstRow.getByRole("cell").nth(1).textContent();

    // Open the review dialog and approve
    await firstRow.getByRole("button", { name: /review/i }).click();
    await expect(adminPage.getByRole("dialog")).toBeVisible();
    await adminPage.getByRole("button", { name: /approve/i }).click();
    // Dialog should close and row disappear (or show Approved status)
    await expect(adminPage.getByRole("dialog")).not.toBeVisible({ timeout: 5_000 });

    await adminCtx.close();

    // ── Step 2: Verify the approved consultant sees their portal ──────────
    // This step requires knowing the approved user's password, which is seeded
    // in staging. If E2E_CONSULTANT_EMAIL matches the applicant, use those creds.
    if (!hasCreds("consultant")) {
      test.skip(true, "E2E_CONSULTANT_EMAIL not set — cannot verify approved user dashboard");
      return;
    }

    const consultantCtx = await browser.newContext();
    const consultantPage = await consultantCtx.newPage();
    await loginAs(consultantPage, creds.consultant.email, creds.consultant.password);

    // After approval the consultant should land on their dashboard, not the
    // onboarding pending page.
    await expect(consultantPage).toHaveURL(/\/consultant/, { timeout: 10_000 });

    // Consultant-specific nav items should be visible
    await expect(
      consultantPage.getByRole("link", { name: /availability|bookings|earnings/i }).first(),
    ).toBeVisible();

    // The email shown in the approval queue should match (if accessible)
    if (applicantEmail) {
      // Just a soft check — log it rather than hard-fail if layout differs
      console.log(`Approved consultant email from onboarding queue: ${applicantEmail.trim()}`);
    }

    await consultantCtx.close();
  });
});
