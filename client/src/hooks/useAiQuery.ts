import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/lib/queryClient";
import { aiApi, type EligibilityDto } from "@/services/api/ai";

// ── Eligibility checker (FR-116/117/118) ──────────────────────────────────────

/**
 * Runs the eligibility check for a scholarship (`POST /api/ai/eligibility/{id}`).
 * Disabled until a scholarship is selected. The query is keyed by id so each
 * checked scholarship keeps its own cached verdict for the session.
 *
 * The check is a POST that counts against the daily AI budget, so window-focus
 * and reconnect refetches are disabled — re-spending the budget silently would
 * be a real bug, not a refresh.
 */
export function useEligibilityQuery(scholarshipId: string | undefined) {
  return useQuery<EligibilityDto>({
    queryKey: queryKeys.ai.eligibility(scholarshipId ?? ""),
    queryFn: () => aiApi.eligibility(scholarshipId ?? ""),
    enabled: !!scholarshipId,
    staleTime: 5 * 60_000,
    gcTime: 10 * 60_000,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    retry: false,
  });
}
