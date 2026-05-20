// PB-004 T-021/T-022 — Applications: route guards, full apply/review/accept lifecycle, withdraw and re-apply
import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

test("student/applications redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/student/applications");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("company/applications-review redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/company/applications-review");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test.describe("PB-004 T-021: full apply → company review → accept lifecycle", () => {
  test.skip(
    !hasCreds("student") || !hasCreds("company"),
    "Requires E2E_STUDENT_EMAIL/PASSWORD + E2E_COMPANY_EMAIL/PASSWORD env vars (seeded staging)"
  );

  test("student applies, saves draft, submits, company accepts, student sees Accepted badge", async ({ browser }) => {
    // --- Student: find a scholarship and apply ---
    const studentCtx = await browser.newContext();
    const studentPage = await studentCtx.newPage();
    await loginAs(studentPage, creds.student.email, creds.student.password);
    await studentPage.goto("/student/scholarships");

    // Click the first open scholarship
    const openScholarship = studentPage
      .getByText(/apply|open/i)
      .first()
      .or(studentPage.getByRole("listitem").first());
    await openScholarship.click();
    await studentPage.getByRole("button", { name: /apply/i }).click();

    // Fill required fields — generic; real fields depend on the form
    const essayField = studentPage.getByLabel(/essay|statement|cover letter/i);
    if (await essayField.isVisible()) {
      await essayField.fill("This is an automated E2E test application. Safe to discard.");
    }

    // Save draft
    await studentPage.getByRole("button", { name: /save draft|draft/i }).click();
    await expect(studentPage.getByText(/draft/i)).toBeVisible({ timeout: 8_000 });

    // Submit
    await studentPage.getByRole("button", { name: /submit application|submit/i }).click();
    await expect(studentPage.getByText(/submitted/i)).toBeVisible({ timeout: 8_000 });

    // Remember the scholarship name to find the application later
    const scholarshipHeading = await studentPage.getByRole("heading", { level: 1 }).textContent();
    await studentCtx.close();

    // --- Company: review and accept ---
    const companyCtx = await browser.newContext();
    const companyPage = await companyCtx.newPage();
    await loginAs(companyPage, creds.company.email, creds.company.password);
    await companyPage.goto("/company/applications-review");

    // Find the application in the queue and open it
    const appRow = companyPage.getByText(scholarshipHeading ?? "").first();
    await expect(appRow).toBeVisible({ timeout: 10_000 });
    await companyPage.getByRole("button", { name: /review/i }).first().click();
    await companyPage.getByRole("button", { name: /accept|approve/i }).click();
    await expect(companyPage.getByText(/accepted|approved/i)).toBeVisible({ timeout: 8_000 });
    await companyCtx.close();

    // --- Student: verify Accepted badge ---
    const studentCtx2 = await browser.newContext();
    const studentPage2 = await studentCtx2.newPage();
    await loginAs(studentPage2, creds.student.email, creds.student.password);
    await studentPage2.goto("/student/applications");
    await expect(studentPage2.getByText(/accepted/i)).toBeVisible({ timeout: 10_000 });
    await studentCtx2.close();
  });
});

test.describe("PB-004 T-022: withdraw and re-apply", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("student can withdraw a submitted application and re-apply to the same scholarship", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/student/applications");

    // Find a Submitted application
    const submittedRow = page.getByText(/submitted/i).first();
    await expect(submittedRow).toBeVisible({ timeout: 8_000 });

    // Withdraw
    await page.getByRole("button", { name: /withdraw/i }).first().click();
    // Confirm dialog if present
    const confirmBtn = page.getByRole("button", { name: /confirm|yes|ok/i });
    if (await confirmBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await confirmBtn.click();
    }
    await expect(page.getByText(/withdrawn/i)).toBeVisible({ timeout: 8_000 });

    // Re-apply to the same scholarship (open the application row detail)
    await page.getByRole("button", { name: /apply again|re-apply|view scholarship/i }).first().click();
    await page.getByRole("button", { name: /apply/i }).click();
    await expect(page.getByText(/draft/i)).toBeVisible({ timeout: 8_000 });
  });
});
