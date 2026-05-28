import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { usePaymentsEnabled, usePlatformStatus } from "@/hooks/usePlatformStatus";
import { apiClient } from "@/services/api/client";

// Stub the API client so we can drive the hook through the same React Query
// pipeline the real App uses, without hitting the network.
vi.mock("@/services/api/client", () => ({
  apiClient: { get: vi.fn() },
}));

function wrapper(qc: QueryClient) {
  return function Wrapper({ children }: PropsWithChildren) {
    return <QueryClientProvider client={qc}>{children}</QueryClientProvider>;
  };
}

function newClient() {
  // retry=false so a mocked rejection becomes a single error, not a loop.
  return new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
}

describe("usePlatformStatus", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns the safe defaults before the query resolves", () => {
    // get() returns a pending promise — the hook should fall back to its
    // hard-coded defaults so the surrounding UI doesn't render wrong.
    (apiClient.get as unknown as ReturnType<typeof vi.fn>).mockReturnValueOnce(
      new Promise(() => { /* never resolves */ }),
    );

    const { result } = renderHook(() => usePlatformStatus(), {
      wrapper: wrapper(newClient()),
    });

    expect(result.current.paymentsEnabled).toBe(true);
    expect(result.current.maintenanceModeEnabled).toBe(false);
  });

  it("reflects paymentsEnabled=false from the server", async () => {
    (apiClient.get as unknown as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      data: {
        maintenanceModeEnabled: false,
        paymentsEnabled: false,
        version: "1.0.0",
        serverTime: new Date().toISOString(),
      },
    });

    const { result } = renderHook(() => usePaymentsEnabled(), {
      wrapper: wrapper(newClient()),
    });

    await waitFor(() => {
      expect(result.current).toBe(false);
    });
  });

  it("reflects paymentsEnabled=true from the server", async () => {
    (apiClient.get as unknown as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      data: {
        maintenanceModeEnabled: false,
        paymentsEnabled: true,
        version: "1.0.0",
        serverTime: new Date().toISOString(),
      },
    });

    const { result } = renderHook(() => usePaymentsEnabled(), {
      wrapper: wrapper(newClient()),
    });

    await waitFor(() => {
      expect(result.current).toBe(true);
    });
  });

  it("falls back to paymentsEnabled=true when the request fails", async () => {
    (apiClient.get as unknown as ReturnType<typeof vi.fn>).mockRejectedValueOnce(
      new Error("network down"),
    );

    const { result } = renderHook(() => usePaymentsEnabled(), {
      wrapper: wrapper(newClient()),
    });

    // The hook never throws — it just returns the safe default. We poll
    // briefly to make sure the failure doesn't flip the value.
    await new Promise((r) => setTimeout(r, 50));
    expect(result.current).toBe(true);
  });
});
