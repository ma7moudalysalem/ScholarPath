import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/lib/queryClient";
import {
  consultantsApi,
  type BookableSlot,
  type ConsultantDetail,
  type ConsultantSummary,
} from "@/services/api/consultants";

// ── Queries ───────────────────────────────────────────────────────────────────

/** Every active consultant with a profile summary, for the browse page. */
export function useConsultantsQuery() {
  return useQuery<ConsultantSummary[]>({
    queryKey: queryKeys.consultants.directory,
    queryFn: () => consultantsApi.browse(),
    staleTime: 60_000,
  });
}

/** One consultant's full public profile detail. */
export function useConsultantDetailQuery(id: string | undefined) {
  return useQuery<ConsultantDetail>({
    queryKey: queryKeys.consultants.detail(id ?? ""),
    queryFn: () => consultantsApi.getById(id ?? ""),
    enabled: !!id,
  });
}

/** A consultant's upcoming bookable slots (next 28 days). */
export function useConsultantAvailabilityQuery(id: string | undefined) {
  return useQuery<BookableSlot[]>({
    queryKey: queryKeys.consultants.availability(id ?? ""),
    queryFn: () => consultantsApi.getAvailability(id ?? ""),
    enabled: !!id,
    staleTime: 30_000,
  });
}
