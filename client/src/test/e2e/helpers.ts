import { type Page } from "@playwright/test";

export async function loginAs(page: Page, email: string, password: string) {
  await page.goto("/login");
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password/i).fill(password);
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL((url) => !url.pathname.startsWith("/login"), { timeout: 10_000 });
}

export const creds = {
  student:    { email: process.env.E2E_STUDENT_EMAIL ?? "",    password: process.env.E2E_STUDENT_PASSWORD ?? "" },
  consultant: { email: process.env.E2E_CONSULTANT_EMAIL ?? "", password: process.env.E2E_CONSULTANT_PASSWORD ?? "" },
  company:    { email: process.env.E2E_COMPANY_EMAIL ?? "",    password: process.env.E2E_COMPANY_PASSWORD ?? "" },
  admin:      { email: process.env.E2E_ADMIN_EMAIL ?? "",      password: process.env.E2E_ADMIN_PASSWORD ?? "" },
};

export function hasCreds(role: keyof typeof creds) {
  return !!(creds[role].email && creds[role].password);
}
