import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/lib/queryClient";
import {
  scholarshipsApi,
  type SearchScholarshipsRequest,
  type ScholarshipListItem,
  type ScholarshipDetail,
  type BookmarkedScholarship,
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

/** The authenticated student's bookmarked scholarships, newest-saved first. */
export function useBookmarksQuery() {
  return useQuery<BookmarkedScholarship[]>({
    queryKey: queryKeys.scholarships.bookmarks,
    queryFn: () => scholarshipsApi.getBookmarks(),
    staleTime: 60_000,
  });
}

/** Featured (Open) scholarships for the home page / dashboards. */
export function useFeaturedScholarshipsQuery(limit?: number) {
  return useQuery<ScholarshipListItem[]>({
    queryKey: limit
      ? [...queryKeys.scholarships.featured, limit]
      : queryKeys.scholarships.featured,
    queryFn: () => scholarshipsApi.getFeatured(limit),
    staleTime: 5 * 60_000,
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useToggleBookmarkMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (scholarshipId: string) =>
      scholarshipsApi.toggleBookmark(scholarshipId),
    onSuccess: () => {
      // `scholarships.all` is the prefix of every scholarship key (lists,
      // detail, bookmarks, featured) — one invalidate refreshes them all.
      void queryClient.invalidateQueries({
        queryKey: queryKeys.scholarships.all,
      });
    },
  });
}
