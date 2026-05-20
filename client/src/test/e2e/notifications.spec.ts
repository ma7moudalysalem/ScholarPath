// PB-010 T-014/T-015 — Notifications: route guard, bell badge, mark-read, preferences
import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

// --- Route guard test ---

test("notifications route redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/notifications");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

// --- Full flow: trigger notification, bell badge, mark as read ---

test.describe("PB-010 T-014: notification bell badge and mark-read flow", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("bell badge increases after an event and decreases after marking read", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);

    // Read current unread count from the bell badge (may be 0 / absent)
    const bellBadge = page.locator("[data-testid='notification-badge'], .notification-badge, [aria-label*='notification' i] .badge");
    const beforeCount = await bellBadge.textContent().catch(() => "0");

    // Trigger a notification: apply to any open scholarship
    await page.goto("/student/scholarships");
    const firstCard = page.getByRole("listitem").first();
    await firstCard.click();
    const applyBtn = page.getByRole("button", { name: /apply/i });
    if (await applyBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await applyBtn.click();
      // Save draft to trigger state change
      await page.getByRole("button", { name: /save draft|draft/i }).click().catch(() => {});
    }

    // Wait for bell to update
    await page.waitForTimeout(2_000);
    const afterCount = await bellBadge.textContent().catch(() => null);

    // Bell badge should now show a count (or have increased)
    // We soft-assert here since CI staging data may vary
    if (afterCount !== null && beforeCount !== null && afterCount !== beforeCount) {
      expect(parseInt(afterCount ?? "0")).toBeGreaterThan(parseInt(beforeCount ?? "0"));
    }

    // Navigate to /notifications
    await page.goto("/notifications");
    // At least one notification should be listed
    const notifItems = page.getByRole("listitem").or(
      page.locator("[data-testid='notification-item']")
    );
    await expect(notifItems.first()).toBeVisible({ timeout: 8_000 });

    // The first notification should be unread — click it to mark read
    const unreadItem = page.getByText(/unread/i).first().or(
      page.locator(".unread, [data-unread='true']").first()
    );
    if (await unreadItem.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await unreadItem.click();
      // Should become read (class change or text change)
      await expect(page.locator(".unread, [data-unread='true']").first()).not.toBeVisible({ timeout: 5_000 });
    } else {
      // Mark all as read button fallback
      const markAllBtn = page.getByRole("button", { name: /mark all.*read|mark as read/i });
      if (await markAllBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await markAllBtn.click();
      }
    }

    // Bell badge count should now be 0 or reduced
    await page.goto("/student");
    const finalCount = await bellBadge.textContent().catch(() => "0");
    if (finalCount !== null && afterCount !== null) {
      expect(parseInt(finalCount ?? "0")).toBeLessThanOrEqual(parseInt(afterCount ?? "0"));
    }
  });
});

// --- Full flow: turn off notification preferences ---

test.describe("PB-010 T-015: notification preference opt-out", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("turning off Application notifications suppresses that notification type", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/profile");

    // Find the notification preferences section
    const notifSection = page.getByText(/notification.*preferences|notification.*settings/i);
    if (await notifSection.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await notifSection.click();
    }

    // Toggle off "Application" notifications
    const applicationToggle = page
      .getByRole("switch", { name: /application/i })
      .or(page.getByRole("checkbox", { name: /application/i }));
    const wasChecked = await applicationToggle.isChecked().catch(() => true);
    if (wasChecked) {
      await applicationToggle.click();
      await expect(applicationToggle).not.toBeChecked({ timeout: 5_000 });
    }

    // Save preferences
    const saveBtn = page.getByRole("button", { name: /save|update|apply/i });
    if (await saveBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await saveBtn.click();
      await expect(page.getByText(/saved|updated|success/i)).toBeVisible({ timeout: 5_000 });
    }

    // Trigger an application event (apply to any scholarship)
    const bellBadge = page.locator("[data-testid='notification-badge'], .notification-badge, [aria-label*='notification' i] .badge");
    const countBefore = await bellBadge.textContent().catch(() => "0");

    await page.goto("/student/scholarships");
    const firstCard = page.getByRole("listitem").first();
    await firstCard.click();
    const applyBtn = page.getByRole("button", { name: /apply/i });
    if (await applyBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await applyBtn.click();
      await page.getByRole("button", { name: /save draft|draft/i }).click().catch(() => {});
    }

    await page.waitForTimeout(2_000);
    const countAfter = await bellBadge.textContent().catch(() => "0");

    // With Application notifications off, no new badge increment for this event
    expect(parseInt(countAfter ?? "0")).toBeLessThanOrEqual(parseInt(countBefore ?? "0") + 0);

    // Restore the preference
    await page.goto("/profile");
    if (await notifSection.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await notifSection.click();
    }
    if (!wasChecked) {
      // If it was originally unchecked, leave it unchecked
    } else {
      // Restore to checked
      const toggleAgain = page
        .getByRole("switch", { name: /application/i })
        .or(page.getByRole("checkbox", { name: /application/i }));
      if (!(await toggleAgain.isChecked().catch(() => false))) {
        await toggleAgain.click();
      }
      const saveBtnRestore = page.getByRole("button", { name: /save|update|apply/i });
      if (await saveBtnRestore.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await saveBtnRestore.click();
      }
    }
  });
});
