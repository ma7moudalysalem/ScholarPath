// PB-003 T-021/T-022 — Scholarships: route guard, student browse/bookmark, company create
import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

test("student/scholarships redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/student/scholarships");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test.describe("full flow: student browses and bookmarks a scholarship", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("student can search scholarships, view detail, and toggle bookmark", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/student/scholarships");

    // Search
    const searchBox = page.getByRole("searchbox").or(page.getByPlaceholder(/search/i));
    await searchBox.fill("engineering");
    // Results should update — wait for at least one card/list-item to appear
    await expect(page.getByRole("list").or(page.locator("[data-testid='scholarship-list']"))).toBeVisible({ timeout: 8_000 });
    const firstCard = page.getByRole("listitem").first().or(
      page.locator("[data-testid='scholarship-card']").first()
    );
    await expect(firstCard).toBeVisible({ timeout: 8_000 });

    // Click into detail
    await firstCard.click();
    // Detail page should show a heading
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible({ timeout: 8_000 });

    // Bookmark toggle
    const bookmarkBtn = page.getByRole("button", { name: /bookmark|save/i }).or(
      page.locator("[aria-label*='bookmark' i]")
    );
    await bookmarkBtn.click();
    // After toggle the button state/icon should change (aria-pressed, aria-label, or class)
    await expect(bookmarkBtn).toHaveAttribute("aria-pressed", "true", { timeout: 5_000 }).catch(async () => {
      // Fallback: assert the button label changed to "remove bookmark" or similar
      await expect(
        page.getByRole("button", { name: /remove bookmark|unbookmark|saved/i }).or(
          page.locator("[aria-label*='bookmarked' i]")
        )
      ).toBeVisible({ timeout: 5_000 });
    });
  });
});

test.describe("full flow: company creates a scholarship", () => {
  test.skip(!hasCreds("company"), "Requires E2E_COMPANY_EMAIL + E2E_COMPANY_PASSWORD env vars (seeded staging)");

  test("company can create a scholarship and see it appear in the list", async ({ page }) => {
    await loginAs(page, creds.company.email, creds.company.password);
    await page.goto("/company/scholarships");

    await page.getByRole("button", { name: /create|add|new scholarship/i }).click();

    const uniqueTitle = `E2E Scholarship ${Date.now()}`;
    await page.getByLabel(/title/i).fill(uniqueTitle);
    await page.getByLabel(/description/i).fill("Automated E2E test scholarship — safe to delete.");

    await page.getByRole("button", { name: /submit|create|save/i }).click();

    // New scholarship should appear in the list
    await expect(page.getByText(uniqueTitle)).toBeVisible({ timeout: 10_000 });
  });
});
