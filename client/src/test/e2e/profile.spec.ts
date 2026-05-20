// PB-002 T-017/T-018 — Profile: route guard, edit profile, change password
import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

test("profile route redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/profile");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test.describe("full flow: edit profile (student)", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("student can update firstName and see the change persist after reload", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/profile");

    const firstNameInput = page.getByLabel(/first name/i);
    await firstNameInput.clear();
    const newName = `Tester${Date.now()}`;
    await firstNameInput.fill(newName);

    await page.getByRole("button", { name: /save|update/i }).click();
    // Wait for success feedback (toast, alert, or any confirmation text)
    await expect(
      page.getByText(/saved|updated|success/i)
    ).toBeVisible({ timeout: 8_000 });

    await page.reload();
    await expect(page.getByLabel(/first name/i)).toHaveValue(newName, { timeout: 5_000 });
  });
});

test.describe("full flow: change password (student)", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("student can change password and log in with the new password", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/profile");

    // Open change-password section (may be a button or already expanded)
    const changePassTrigger = page.getByRole("button", { name: /change password/i }).or(
      page.getByText(/change password/i)
    );
    if (await changePassTrigger.isVisible()) {
      await changePassTrigger.click();
    }

    const currentPassInput = page.getByLabel(/current password/i);
    await currentPassInput.fill(creds.student.password);

    const newPassword = `NewPass${Date.now()}!`;
    await page.getByLabel(/new password/i).fill(newPassword);
    await page.getByLabel(/confirm.*password|repeat.*password/i).fill(newPassword);

    await page.getByRole("button", { name: /update password|save password|change/i }).click();
    await expect(
      page.getByText(/password.*changed|updated|success/i)
    ).toBeVisible({ timeout: 8_000 });

    // Sign out
    const signOutBtn = page.getByRole("button", { name: /sign out|log out|logout/i }).or(
      page.getByRole("link", { name: /sign out|log out|logout/i })
    );
    await signOutBtn.click();
    await page.waitForURL((url) => url.pathname === "/" || url.pathname.startsWith("/login"), {
      timeout: 8_000,
    });

    // Log in with the new password — should succeed
    await loginAs(page, creds.student.email, newPassword);
    await expect(page).toHaveURL(/\/student/, { timeout: 8_000 });

    // Restore original password so the test account stays usable
    await page.goto("/profile");
    if (await changePassTrigger.isVisible()) {
      await changePassTrigger.click();
    }
    await page.getByLabel(/current password/i).fill(newPassword);
    await page.getByLabel(/new password/i).fill(creds.student.password);
    await page.getByLabel(/confirm.*password|repeat.*password/i).fill(creds.student.password);
    await page.getByRole("button", { name: /update password|save password|change/i }).click();
    await expect(
      page.getByText(/password.*changed|updated|success/i)
    ).toBeVisible({ timeout: 8_000 });
  });
});
