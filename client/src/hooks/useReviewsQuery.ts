import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/lib/queryClient";
import { reviewsApi, type ReceivedReviewsSummary } from "@/services/api/reviews";

// ── "Reviews received" queries ────────────────────────────────────────────────

/** Reviews received by the authenticated company (aggregate + list). */
export function useScholarshipProviderReceivedReviewsQuery() {
  return useQuery<ReceivedReviewsSummary>({
    queryKey: queryKeys.reviews.scholarshipProviderMine,
    queryFn: () => reviewsApi.scholarshipProviderMine(),
    staleTime: 60_000,
  });
}

/** Reviews received by the authenticated consultant (aggregate + list). */
export function useConsultantReceivedReviewsQuery() {
  return useQuery<ReceivedReviewsSummary>({
    queryKey: queryKeys.reviews.consultantMine,
    queryFn: () => reviewsApi.consultantMine(),
    staleTime: 60_000,
  });
}
