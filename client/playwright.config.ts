import { defineConfig, devices } from "@playwright/test";

// When E2E_BASE_URL is set the tests target a remote environment (staging,
// preview, CI) and we must NOT spin up a local dev server.
const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:5173";
const isRemote = !!process.env.E2E_BASE_URL;

export default defineConfig({
  testDir: "./src/test/e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI
    ? [
        ["html", { open: "never" }],
        ["junit", { outputFile: "playwright-report/junit.xml" }],
        ["github"],
      ]
    : [["html", { open: "never" }], ["list"]],
  use: {
    baseURL,
    trace: "on-first-retry",
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    { name: "firefox", use: { ...devices["Desktop Firefox"] } },
    { name: "mobile", use: { ...devices["Pixel 7"] } },
  ],
  // Only start the dev server for local runs; skip it when targeting a remote URL.
  webServer: isRemote
    ? undefined
    : {
        command: "npm run dev",
        url: "http://localhost:5173",
        reuseExistingServer: !process.env.CI,
        timeout: 60_000,
      },
});
