// PB-006 T-023/T-024 — Bookings: route guards, full booking lifecycle, cancellation flow
import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

// --- Route guard tests (always run, no credentials needed) ---

test("student/consultants redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/student/consultants");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("student/bookings redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/student/bookings");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("consultant/bookings redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/consultant/bookings");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

// --- Full flow: happy path ---

test.describe("PB-006 T-023: full booking happy path (book → accept → complete → rate)", () => {
  test.skip(
    !hasCreds("student") || !hasCreds("consultant"),
    "Requires E2E_STUDENT_EMAIL/PASSWORD + E2E_CONSULTANT_EMAIL/PASSWORD env vars (seeded staging)"
  );

  test("student books, consultant accepts, student completes and rates", async ({ browser }) => {
    // --- Student: browse consultants and book ---
    const studentCtx = await browser.newContext();
    const studentPage = await studentCtx.newPage();
    await loginAs(studentPage, creds.student.email, creds.student.password);
    await studentPage.goto("/student/consultants");

    // Click the first consultant card
    await studentPage.getByRole("listitem").first().click();

    // Pick the first available slot
    const firstSlot = studentPage
      .getByRole("button", { name: /available|book|pick/i })
      .first()
      .or(studentPage.locator("[data-testid='time-slot']").first());
    await firstSlot.click();

    // Fill optional notes
    const notesField = studentPage.getByLabel(/notes|message/i);
    if (await notesField.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await notesField.fill("E2E automated booking — safe to discard.");
    }

    // Confirm booking — Stripe test card 4242 4242 4242 4242
    const cardFrame = studentPage.frameLocator("iframe[name*='stripe' i], iframe[src*='stripe' i]").first();
    const cardInput = cardFrame.getByPlaceholder(/card number/i);
    if (await cardInput.isVisible({ timeout: 5_000 }).catch(() => false)) {
      await cardInput.fill("4242424242424242");
      await cardFrame.getByPlaceholder(/MM \/ YY|expiry/i).fill("12/28");
      await cardFrame.getByPlaceholder(/CVC/i).fill("123");
      await cardFrame.getByPlaceholder(/ZIP|postal/i).fill("10001").catch(() => { /* optional */ });
    }
    await studentPage.getByRole("button", { name: /confirm|pay|book/i }).click();

    // Assert Pending badge
    await expect(studentPage.getByText(/pending/i)).toBeVisible({ timeout: 15_000 });
    await studentCtx.close();

    // --- Consultant: accept the booking ---
    const consultantCtx = await browser.newContext();
    const consultantPage = await consultantCtx.newPage();
    await loginAs(consultantPage, creds.consultant.email, creds.consultant.password);
    await consultantPage.goto("/consultant/bookings");

    await consultantPage.getByRole("button", { name: /accept/i }).first().click();
    await expect(consultantPage.getByText(/confirmed/i)).toBeVisible({ timeout: 8_000 });
    await consultantCtx.close();

    // --- Student: assert Confirmed, mark Complete, submit rating ---
    const studentCtx2 = await browser.newContext();
    const studentPage2 = await studentCtx2.newPage();
    await loginAs(studentPage2, creds.student.email, creds.student.password);
    await studentPage2.goto("/student/bookings");

    await expect(studentPage2.getByText(/confirmed/i)).toBeVisible({ timeout: 8_000 });

    // Mark as complete
    await studentPage2.getByRole("button", { name: /complete|mark.*complete/i }).first().click();
    await expect(studentPage2.getByText(/completed/i)).toBeVisible({ timeout: 8_000 });

    // Submit 5-star rating
    const stars = studentPage2.getByRole("radio", { name: /5|five star/i }).or(
      studentPage2.locator("[aria-label='5 stars'], [data-value='5']")
    );
    if (await stars.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await stars.click();
    } else {
      // Fallback: click the 5th star element
      await studentPage2.locator("[data-testid='star'], .star, [aria-label*='star' i]").nth(4).click();
    }
    await studentPage2.getByRole("button", { name: /submit.*rating|rate|send/i }).click();

    // Rating should now be visible somewhere on the booking row
    await expect(studentPage2.getByText(/5|five/i).first()).toBeVisible({ timeout: 8_000 });
    await studentCtx2.close();
  });
});

// --- Full flow: cancellation before acceptance ---

test.describe("PB-006 T-024: student cancels booking before consultant accepts", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("student can cancel a pending booking and sees Cancelled status", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/student/consultants");

    // Book the first consultant
    await page.getByRole("listitem").first().click();
    const firstSlot = page
      .getByRole("button", { name: /available|book|pick/i })
      .first()
      .or(page.locator("[data-testid='time-slot']").first());
    await firstSlot.click();

    const cardFrame = page.frameLocator("iframe[name*='stripe' i], iframe[src*='stripe' i]").first();
    const cardInput = cardFrame.getByPlaceholder(/card number/i);
    if (await cardInput.isVisible({ timeout: 5_000 }).catch(() => false)) {
      await cardInput.fill("4242424242424242");
      await cardFrame.getByPlaceholder(/MM \/ YY|expiry/i).fill("12/28");
      await cardFrame.getByPlaceholder(/CVC/i).fill("123");
    }
    await page.getByRole("button", { name: /confirm|pay|book/i }).click();
    await expect(page.getByText(/pending/i)).toBeVisible({ timeout: 15_000 });

    // Navigate to bookings and cancel
    await page.goto("/student/bookings");
    await expect(page.getByText(/pending/i)).toBeVisible({ timeout: 8_000 });
    await page.getByRole("button", { name: /cancel/i }).first().click();

    // Confirm dialog if present
    const confirmBtn = page.getByRole("button", { name: /confirm|yes|ok/i });
    if (await confirmBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await confirmBtn.click();
    }

    await expect(page.getByText(/cancelled|canceled/i)).toBeVisible({ timeout: 8_000 });
  });
});
