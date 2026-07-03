import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/services/api/client";

/**
 * Shape returned by `GET /api/status`. The endpoint is always anonymous and
 * cheap — the same query is mounted by `App.tsx` for the maintenance gate, so
 * this hook only re-uses that already-cached data via the shared query key.
 */
export interface PlatformStatus {
  maintenanceModeEnabled: boolean;
  /**
   * Master payments switch. When false the platform runs fully free —
   * fee inputs are hidden, billing dashboards show a banner, Apply Now /
   * Booking always take the free path.
   */
  paymentsEnabled: boolean;
  version: string;
  serverTime: string;
}

/**
 * Subscribes to the cached `/api/status` query so any component can read the
 * platform flags. Defaults to `paymentsEnabled = true` and
 * `maintenanceModeEnabled = false` until the first response lands — this
 * matches the "don't break the app when the status endpoint is slow" stance
 * the App-level guard already takes.
 *
 * This hook is a pure *reader* of the shared `["platform","status"]` cache — it
 * deliberately does NOT set `refetchInterval`/`staleTime: 0`. The single poll
 * lives in `App.tsx` (always mounted at the root), so all 25+ consumers here
 * share one background request instead of each scheduling their own. Without
 * this, a page mounting several fee-gated components — plus `staleTime: 0`
 * forcing a fresh fetch on every mount — hammered `/api/status` far more than
 * the intended once-per-poll. The global 60 s `staleTime` (queryClient.ts)
 * keeps mounts serving from cache.
 */
export function usePlatformStatus(): PlatformStatus {
  const { data } = useQuery<PlatformStatus>({
    queryKey: ["platform", "status"],
    queryFn: async () => {
      const { data } = await apiClient.get<PlatformStatus>("/api/status");
      return data;
    },
    retry: false,
  });

  return data ?? {
    maintenanceModeEnabled: false,
    paymentsEnabled: true,
    version: "0.0.0",
    serverTime: new Date().toISOString(),
  };
}

/** Shorthand for the most common consumer — gates fee UI and Stripe paths. */
export function usePaymentsEnabled(): boolean {
  return usePlatformStatus().paymentsEnabled;
}
