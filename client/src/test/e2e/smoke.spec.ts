import { test, expect } from "@playwright/test";

test("home renders + language toggles direction", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();

  // Toggle to AR
  await page.getByRole("button", { name: /language|اللغة/i }).click();
  await expect(page.locator("html")).toHaveAttribute("dir", "rtl");

  // Toggle back to EN
  await page.getByRole("button", { name: /language|اللغة/i }).click();
  await expect(page.locator("html")).toHaveAttribute("dir", "ltr");
});

test("login form renders with all fields", async ({ page }) => {
  await page.goto("/login");
  await expect(page.getByLabel(/email/i)).toBeVisible();
  await expect(page.getByLabel(/password/i)).toBeVisible();
  await expect(page.getByRole("button", { name: /sign in/i })).toBeVisible();
});
