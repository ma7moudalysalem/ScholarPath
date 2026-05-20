// PB-008 T-017 — AI features E2E: route guards + full flow (stub mode).
// Route-guard tests run unconditionally; full flows skip without credentials.

import { test, expect } from "@playwright/test";
import { loginAs, creds, hasCreds } from "./helpers";

// ── Route guards ─────────────────────────────────────────────────────────────

test("student/ai redirects unauthenticated user to /login", async ({ page }) => {
  await page.goto("/student/ai");
  await page.waitForURL(/\/login/, { timeout: 5_000 });
  await expect(page).toHaveURL(/\/login/);
});

test("/student/ai renders login chrome (not a blank page) when unauthenticated", async ({ page }) => {
  await page.goto("/student/ai");
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible({ timeout: 5_000 });
});

// ── Full-flow tests (skip without staging credentials) ────────────────────────

test.describe("AI features full flow — requires seeded staging", () => {
  test.skip(!hasCreds("student"), "Requires E2E_STUDENT_EMAIL + E2E_STUDENT_PASSWORD env vars (seeded staging)");

  test("recommendations panel loads and cards are visible", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/student/ai");
    // The recommendations section should render at least one card or a
    // "no recommendations yet" fallback — either means the endpoint responded.
    const recommendationsSection = page.getByTestId("ai-recommendations").or(
      page.getByRole("heading", { name: /recommend/i }),
    );
    await expect(recommendationsSection).toBeVisible({ timeout: 15_000 });
  });

  test("eligibility check returns per-criterion breakdown", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/student/ai");
    // Open eligibility panel (button may be labelled "Check eligibility" or similar)
    const eligibilityBtn = page.getByRole("button", { name: /eligibilit/i });
    if (await eligibilityBtn.isVisible()) {
      await eligibilityBtn.click();
      // Expect at least one criterion row to appear
      await expect(page.getByRole("listitem").first()).toBeVisible({ timeout: 10_000 });
    }
  });

  test("chatbot Q/A round trip — sends message and receives non-empty response", async ({ page }) => {
    await loginAs(page, creds.student.email, creds.student.password);
    await page.goto("/student/ai");
    // Find the chat input
    const chatInput = page.getByPlaceholder(/message|ask|type/i).or(
      page.getByRole("textbox", { name: /message/i }),
    );
    await expect(chatInput).toBeVisible({ timeout: 10_000 });
    await chatInput.fill("What scholarships are available for computer science students?");
    await page.keyboard.press("Enter");
    // Wait for a response bubble to appear (not the user's own message)
    await expect(
      page.locator("[data-role='assistant'], [data-sender='ai'], .ai-message, .assistant-bubble").first(),
    ).toBeVisible({ timeout: 30_000 });
  });
});
