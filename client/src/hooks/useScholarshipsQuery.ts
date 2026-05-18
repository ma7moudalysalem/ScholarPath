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

/**
 * Flips `isBookmarked` for one scholarship across whatever shape a cached
 * scholarship query holds — a paginated list, a flat array (featured /
 * bookmarks), or a single detail object — so an optimistic toggle shows up
 * everywhere the scholarship is rendered at once.
 */
function flipBookmarkInCache(cached: unknown, scholarshipId: string): unknown {
  const flip = <T extends ScholarshipListItem>(item: T): T =>
    item.id === scholarshipId ? { ...item, isBookmarked: !item.isBookmarked } : item;

  if (!cached || typeof cached !== "object") return cached;

  // Paginated<ScholarshipListItem> — the browse/search lists.
  if ("items" in cached && Array.isArray((cached as Paginated<ScholarshipListItem>).items)) {
    const list = cached as Paginated<ScholarshipListItem>;
    return { ...list, items: list.items.map(flip) };
  }

  // Flat array — featured (ScholarshipListItem[]) or bookmarks (BookmarkedScholarship[]).
  if (Array.isArray(cached)) {
    return (cached as unknown[]).map((entry) => {
      if (entry && typeof entry === "object" && "scholarship" in entry) {
        const bm = entry as BookmarkedScholarship;
        return bm.scholarship.id === scholarshipId
          ? { ...bm, scholarship: flip(bm.scholarship) }
          : bm;
      }
      return flip(entry as ScholarshipListItem);
    });
  }

  // A single ScholarshipDetail object.
  if ("id" in cached && "isBookmarked" in cached) {
    return flip(cached as ScholarshipDetail);
  }

  return cached;
}

export function useToggleBookmarkMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (scholarshipId: string) =>
      scholarshipsApi.toggleBookmark(scholarshipId),
    // Optimistically flip the bookmark icon everywhere the scholarship shows.
    onMutate: async (scholarshipId: string) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.scholarships.all });
      const snapshot = queryClient.getQueriesData({ queryKey: queryKeys.scholarships.all });
      queryClient.setQueriesData(
        { queryKey: queryKeys.scholarships.all },
        (cached: unknown) => flipBookmarkInCache(cached, scholarshipId),
      );
      return { snapshot };
    },
    onError: (_err, _scholarshipId, context) => {
      // Toggle failed — restore the pre-toggle cache snapshot.
      context?.snapshot.forEach(([key, value]) => queryClient.setQueryData(key, value));
    },
    onSettled: () => {
      // `scholarships.all` is the prefix of every scholarship key (lists,
      // detail, bookmarks, featured) — one invalidate reconciles them all.
      void queryClient.invalidateQueries({ queryKey: queryKeys.scholarships.all });
    },
  });
}
