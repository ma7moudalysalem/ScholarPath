import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/lib/queryClient";
import {
  scholarshipsApi,
  type SearchScholarshipsRequest,
  type ScholarshipListItem,
  type ScholarshipDetail,
  type Paginated,
} from "@/services/api/scholarships";

// ── Queries ───────────────────────────────────────────────────────────────────

export function useScholarshipsQuery(req: SearchScholarshipsRequest) {
  return useQuery<Paginated<ScholarshipListItem>>({
    queryKey: queryKeys.scholarships.list({ ...req }),
    queryFn: () => scholarshipsApi.search(req),
    staleTime: 30_000,
    placeholderData: (prev) => prev,
  });
}

export function useScholarshipDetailQuery(id: string | undefined) {
  return useQuery<ScholarshipDetail>({
    queryKey: queryKeys.scholarships.detail(id ?? ""),
    queryFn: () => scholarshipsApi.getById(id ?? ""),
    enabled: !!id,
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useToggleBookmarkMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (scholarshipId: string) =>
      scholarshipsApi.toggleBookmark(scholarshipId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: queryKeys.scholarships.all,
      });
    },
  });
}
