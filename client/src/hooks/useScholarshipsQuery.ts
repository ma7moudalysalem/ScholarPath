import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/lib/queryClient";
import {
  scholarshipsApi,
  type SearchScholarshipsRequest,
  type ScholarshipListItem,
  type Paginated,
} from "@/services/api/scholarships";

/**
 * REFERENCE QUERY HOOK — copy this pattern for every read operation in your module.
 * Demonstrates:
 *   - useQuery with typed payload
 *   - queryKey factory usage (do NOT inline string arrays)
 *   - staleTime per-query override (when search is expensive to rerun)
 *   - placeholder data for snappy pagination
 */
export function useScholarshipsQuery(req: SearchScholarshipsRequest) {
  return useQuery<Paginated<ScholarshipListItem>>({
    queryKey: queryKeys.scholarships.list({ ...req }),
    queryFn: () => scholarshipsApi.search(req),
    staleTime: 30_000,
    placeholderData: (prev) => prev,
  });
}

export function useScholarshipDetailQuery(id: string | undefined) {
  return useQuery<ScholarshipListItem>({
    queryKey: queryKeys.scholarships.detail(id ?? ""),
    queryFn: () => scholarshipsApi.getById(id ?? ""),
    enabled: !!id,
  });
}

/**
 * REFERENCE MUTATION HOOK — copy this pattern for every write operation.
 * Demonstrates:
 *   - invalidating affected queries on success
 *   - optimistic update via setQueryData
 */
export function useToggleBookmarkMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (scholarshipId: string) => scholarshipsApi.toggleBookmark(scholarshipId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.scholarships.bookmarks });
      void queryClient.invalidateQueries({ queryKey: queryKeys.scholarships.all });
    },
  });
}
