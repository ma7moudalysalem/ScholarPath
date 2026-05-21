import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import "@/lib/i18n";

// `authApi` is mocked at the module level so submit doesn't actually call the API.
vi.mock("@/services/api/auth", () => ({
  authApi: {
    resetPassword: vi.fn(async () => undefined),
  },
}));

// Avoid real toasts blowing up the JSDOM render.
vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

import { ResetPassword } from "@/pages/auth/ResetPassword";
import { authApi } from "@/services/api/auth";

function renderWithToken(token: string | null) {
  const initialEntries = token ? [`/reset-password?token=${token}`] : ["/reset-password"];
  return render(
    <MemoryRouter initialEntries={initialEntries}>
      <ResetPassword />
    </MemoryRouter>,
  );
}

describe("ResetPassword — password rules mirror Register", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows the invalid-link state when the token is missing", () => {
    renderWithToken(null);
    expect(screen.getByRole("heading", { level: 1 })).toBeInTheDocument();
    // No password fields rendered without a token.
    expect(screen.queryByLabelText(/new password/i)).not.toBeInTheDocument();
  });

  it("rejects passwords that are too short", async () => {
    renderWithToken("abc");
    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/new password/i), "Aa1!");
    await user.type(screen.getByLabelText(/confirm/i), "Aa1!");
    fireEvent.submit(screen.getByRole("button", { name: /reset/i }).closest("form")!);
    await waitFor(() => {
      expect(authApi.resetPassword).not.toHaveBeenCalled();
    });
  });

  it("rejects passwords missing an uppercase letter, digit, or special char", async () => {
    renderWithToken("abc");
    const user = userEvent.setup();
    // Long enough but all lowercase letters — should fail upper + digit + special checks.
    await user.type(screen.getByLabelText(/new password/i), "abcdefghij");
    await user.type(screen.getByLabelText(/confirm/i), "abcdefghij");
    fireEvent.submit(screen.getByRole("button", { name: /reset/i }).closest("form")!);
    await waitFor(() => {
      expect(authApi.resetPassword).not.toHaveBeenCalled();
    });
  });

  it("rejects mismatched confirmation", async () => {
    renderWithToken("abc");
    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/new password/i), "Abcdef1!");
    await user.type(screen.getByLabelText(/confirm/i), "Different1!");
    fireEvent.submit(screen.getByRole("button", { name: /reset/i }).closest("form")!);
    await waitFor(() => {
      expect(authApi.resetPassword).not.toHaveBeenCalled();
    });
  });

  it("accepts a valid password that matches the confirmation", async () => {
    renderWithToken("abc");
    const user = userEvent.setup();
    await user.type(screen.getByLabelText(/new password/i), "Abcdef1!");
    await user.type(screen.getByLabelText(/confirm/i), "Abcdef1!");
    fireEvent.submit(screen.getByRole("button", { name: /reset/i }).closest("form")!);
    await waitFor(() => {
      expect(authApi.resetPassword).toHaveBeenCalledWith("abc", "Abcdef1!");
    });
  });
});
