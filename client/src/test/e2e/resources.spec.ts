// PB-009 T-014 — Resources: route guards, author submit for review, admin approve, student views
import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

// --- Route guard tests ---

test("author/resources redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/author/resources");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("student/resources redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/student/resources");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

// --- Full flow: create resource → approve → student views ---

test.describe("PB-009 T-014: consultant creates resource, admin approves, student sees it", () => {
  test.skip(
    !hasCreds("consultant") || !hasCreds("admin") || !hasCreds("student"),
    "Requires E2E_CONSULTANT_EMAIL/PASSWORD + E2E_ADMIN_EMAIL/PASSWORD + E2E_STUDENT_EMAIL/PASSWORD env vars (seeded staging)"
  );

  test("full resource lifecycle from draft to published", async ({ browser }) => {
    const uniqueTitle = `E2E Resource ${Date.now()}`;

    // --- Consultant/Author: create resource and submit for review ---
    const authorCtx = await browser.newContext();
    const authorPage = await authorCtx.newPage();
    await loginAs(authorPage, creds.consultant.email, creds.consultant.password);
    await authorPage.goto("/author/resources");

    await authorPage.getByRole("button", { name: /create|new resource|add/i }).click();

    // Title — English
    const titleEnInput = authorPage.getByLabel(/title.*en|english.*title|title/i).first();
    await titleEnInput.fill(uniqueTitle);

    // Title — Arabic (may be optional or a separate input)
    const titleArInput = authorPage.getByLabel(/title.*ar|arabic.*title/i);
    if (await titleArInput.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await titleArInput.fill(`مورد اختباري ${Date.now()}`);
    }

    // Content body
    const contentInput = authorPage.getByLabel(/content|body|text/i).or(
      authorPage.getByRole("textbox").nth(1)
    );
    await contentInput.fill("Automated E2E test resource content. Safe to delete.");

    // Save as draft first
    await authorPage.getByRole("button", { name: /save draft|draft/i }).click();
    await expect(authorPage.getByText(/draft/i)).toBeVisible({ timeout: 8_000 });

    // Submit for review
    await authorPage.getByRole("button", { name: /submit.*review|submit for review/i }).click();
    await expect(authorPage.getByText(/pending.*review|under review|submitted/i)).toBeVisible({ timeout: 8_000 });
    await authorCtx.close();

    // --- Admin: find resource in queue and approve ---
    const adminCtx = await browser.newContext();
    const adminPage = await adminCtx.newPage();
    await loginAs(adminPage, creds.admin.email, creds.admin.password);
    await adminPage.goto("/admin/articles");

    // Find the pending resource
    await expect(adminPage.getByText(uniqueTitle)).toBeVisible({ timeout: 10_000 });
    // Click Approve on the resource row
    const resourceRow = adminPage.locator("tr, [data-testid='resource-row']").filter({ hasText: uniqueTitle });
    await resourceRow.getByRole("button", { name: /approve/i }).click();
    await expect(adminPage.getByText(/approved|published/i)).toBeVisible({ timeout: 8_000 });
    await adminCtx.close();

    // --- Student: verify the resource is visible and published ---
    const studentCtx = await browser.newContext();
    const studentPage = await studentCtx.newPage();
    await loginAs(studentPage, creds.student.email, creds.student.password);
    await studentPage.goto("/student/resources");

    await expect(studentPage.getByText(uniqueTitle)).toBeVisible({ timeout: 10_000 });
    // The resource card/row should show a Published badge or similar indicator
    const resourceEntry = studentPage.locator("*").filter({ hasText: uniqueTitle }).first();
    await expect(
      resourceEntry.getByText(/published/i).or(
        studentPage.getByText(/published/i).first()
      )
    ).toBeVisible({ timeout: 5_000 });
    await studentCtx.close();
  });
});
