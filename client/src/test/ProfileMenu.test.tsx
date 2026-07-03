import { describe, it, expect, vi, beforeEach, beforeAll } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import i18n from "@/lib/i18n";
import type * as AuthApiModule from "@/services/api/auth";

beforeAll(async () => {
  // Default fallback language is Arabic — force English so label regexes match.
  await i18n.changeLanguage("en");
});

// Stub the notifications API the layout calls when mounting.
vi.mock("@/services/api/notifications", () => ({
  notificationsApi: { unreadCount: vi.fn(async () => 0) },
  UNREAD_COUNT_QUERY_KEY: ["notifications", "unread-count"],
}));

vi.mock("@/hooks/useNotificationHub", () => ({
  useNotificationHub: () => undefined,
}));

// vi.mock factories are hoisted, so the spy must live inside vi.hoisted() to
// exist by the time the mock factory runs.
const { switchRoleSpy } = vi.hoisted(() => ({
  switchRoleSpy: vi.fn(async () => ({
    accessToken: "new-access",
    refreshToken: "new-refresh",
    accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
    refreshTokenExpiresAt: new Date(Date.now() + 7 * 86_400_000).toISOString(),
    user: {
      id: "user-1",
      email: "u@example.com",
      firstName: "Test",
      lastName: "User",
      fullName: "Test User",
      profileImageUrl: null,
      accountStatus: "Active" as const,
      isOnboardingComplete: true,
      emailConfirmed: true,
      roles: ["Student", "Consultant"],
      activeRole: "Consultant",
      preferredLanguage: "en",
    },
  })),
}));

vi.mock("@/services/api/auth", async () => {
  const actual = await vi.importActual<typeof AuthApiModule>("@/services/api/auth");
  return {
    ...actual,
    authApi: {
      ...actual.authApi,
      switchRole: switchRoleSpy,
    },
  };
});

vi.mock("sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

import { AuthenticatedLayout } from "@/components/layout/AuthenticatedLayout";
import { useAuthStore } from "@/stores/authStore";

function seedUser(roles: string[], activeRole: string) {
  useAuthStore.setState({
    user: {
      id: "user-1",
      email: "u@example.com",
      firstName: "Test",
      lastName: "User",
      fullName: "Test User",
      profileImageUrl: null,
      accountStatus: "Active",
      isOnboardingComplete: true,
      emailConfirmed: true,
      roles,
      activeRole,
      preferredLanguage: "en",
    },
    tokens: {
      accessToken: "a",
      refreshToken: "r",
      accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      refreshTokenExpiresAt: new Date(Date.now() + 86_400_000).toISOString(),
    },
    isHydrated: true,
  });
}

function renderLayout() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <AuthenticatedLayout />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("ProfileMenu — role switcher", () => {
  beforeEach(() => {
    switchRoleSpy.mockClear();
    useAuthStore.setState({ user: null, tokens: null, isHydrated: true });
  });

  it("does not show switch options for a single-role user", async () => {
    seedUser(["Student"], "Student");
    renderLayout();
    const trigger = await screen.findByRole("button", { name: /profile/i });
    await userEvent.click(trigger);
    // Identity card visible.
    expect(screen.getByText("u@example.com")).toBeInTheDocument();
    // No "Switch to …" item rendered.
    expect(screen.queryByRole("menuitem", { name: /switch to/i })).not.toBeInTheDocument();
  });

  it("shows the other role as a switch option for a dual-role user", async () => {
    seedUser(["Student", "Consultant"], "Student");
    renderLayout();
    const trigger = await screen.findByRole("button", { name: /profile/i });
    await userEvent.click(trigger);
    // Only the non-active role appears as a switch option.
    expect(screen.getByRole("menuitem", { name: /switch to consultant/i })).toBeInTheDocument();
    expect(screen.queryByRole("menuitem", { name: /switch to student/i })).not.toBeInTheDocument();
  });

  it("calls the switch-role API when a target role is picked", async () => {
    seedUser(["Student", "Consultant"], "Student");
    renderLayout();
    const trigger = await screen.findByRole("button", { name: /profile/i });
    await userEvent.click(trigger);
    await userEvent.click(screen.getByRole("menuitem", { name: /switch to consultant/i }));
    expect(switchRoleSpy).toHaveBeenCalledWith("Consultant");
  });
});
