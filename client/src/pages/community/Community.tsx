import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import {
  MessageSquare,
  Search,
  Plus,
  ArrowUp,
  ArrowDown,
  Clock,
  TrendingUp,
  Filter,
  ChevronRight,
  Hash,
  Flame,
  Bookmark,
} from "lucide-react";
import { motion } from "motion/react";
import { communityApi, forumPostBody, forumPostTitle, type ForumCategory, type ForumPost, type VoteType } from "@/services/api/community";
import { AskQuestionModal } from "@/components/community/AskQuestionModal";
import { UserAvatar } from "@/components/common/UserAvatar";
import { Link } from "react-router";
import { formatDistanceToNow } from "date-fns";
import { ar } from "date-fns/locale";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { toast } from "sonner";
import { useAuthStore } from "@/stores/authStore";
import { apiErrorMessage } from "@/services/api/client";
import { EmptyState } from "@/components/ui/EmptyState";
import { createTypedCommunityHub } from "@/services/signalR/communityHub";

type FeedTab = "all" | "bookmarks";

export function Community() {
  const { t, i18n } = useTranslation("community");
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isRtl ? ar : undefined;

  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>();
  const [selectedTag, setSelectedTag] = useState<string | undefined>();
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState("Newest");
  const [tab, setTab] = useState<FeedTab>("all");
  const [askOpen, setAskOpen] = useState(false);

  const activeRole = useAuthStore((state) => state.user?.activeRole);
  const roles = useAuthStore((state) => state.user?.roles ?? []);
  const isStudent = activeRole === "Student" || roles.includes("Student");

  const { data: categories = [] } = useQuery<ForumCategory[]>({
    queryKey: ["community", "categories"],
    queryFn: () => communityApi.getCategories(),
  });

  const feedQueryKey = useMemo(
    () => [
      "community",
      tab === "bookmarks" ? "bookmarks-feed" : "posts",
      tab === "bookmarks" ? null : selectedCategoryId ?? null,
      tab === "bookmarks" ? null : searchQuery,
      tab === "bookmarks" ? null : sortBy,
      tab === "bookmarks" ? null : selectedTag ?? null,
    ] as const,
    [tab, selectedCategoryId, searchQuery, sortBy, selectedTag],
  );

  const { data: postsData, isLoading: loading } = useQuery({
    queryKey: feedQueryKey,
    queryFn: () =>
      tab === "bookmarks"
        ? communityApi.getMyBookmarks(1, 20)
        : communityApi.getPosts({
            categoryId: selectedCategoryId,
            query: searchQuery,
            sortBy,
            tag: selectedTag,
            page: 1,
            pageSize: 20,
          }),
    placeholderData: keepPreviousData,
    enabled: tab !== "bookmarks" || isStudent,
  });

  const posts = postsData?.items ?? [];

  // Trending: top 3 most-voted posts in the last 30 days, fetched independently
  // of the filtered/sorted feed below so the strip is stable as the user
  // changes categories. Hidden on filter / search to avoid duplicating context.
  const { data: trendingData } = useQuery({
    queryKey: ["community", "trending"],
    queryFn: () => communityApi.getPosts({ sortBy: "Trending", page: 1, pageSize: 6 }),
    staleTime: 60_000,
  });
  const trendingPosts = useMemo<ForumPost[]>(() => {
    const items = trendingData?.items ?? [];
    return items.slice(0, 3);
  }, [trendingData]);
  const showTrending =
    tab === "all" &&
    !selectedCategoryId &&
    !selectedTag &&
    searchQuery.trim().length === 0 &&
    trendingPosts.length > 0;

  const qc = useQueryClient();
  const voteMutation = useMutation({
    mutationFn: ({ postId, type }: { postId: string; type: VoteType }) =>
      communityApi.toggleVote(postId, type),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["community", "posts"] });
      void qc.invalidateQueries({ queryKey: ["community", "bookmarks-feed"] });
      void qc.invalidateQueries({ queryKey: ["community", "trending"] });
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("actions.voteError"))),
  });

  const bookmarkMutation = useMutation({
    mutationFn: (postId: string) => communityApi.toggleBookmark(postId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["community", "posts"] });
      void qc.invalidateQueries({ queryKey: ["community", "bookmarks-feed"] });
      void qc.invalidateQueries({ queryKey: ["community", "thread"] });
      void qc.invalidateQueries({ queryKey: ["community", "trending"] });
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("actions.bookmarkError"))),
  });

  // The vote buttons sit inside the post Link — block navigation when voting.
  const handleVote = (e: React.MouseEvent, postId: string, type: VoteType) => {
    e.preventDefault();
    e.stopPropagation();
    voteMutation.mutate({ postId, type });
  };

  const handleBookmark = (e: React.MouseEvent, postId: string) => {
    e.preventDefault();
    e.stopPropagation();
    bookmarkMutation.mutate(postId);
  };

  const handleTagClick = (e: React.MouseEvent, tag: string) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedTag(tag);
    setTab("all");
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    // Search is handled by the query dependency on searchQuery
  };

  // ── Real-time: join the active category group and invalidate on PostCreated.
  useEffect(() => {
    let cancelled = false;
    const hub = createTypedCommunityHub();
    const handlePostCreated = () => {
      // Invalidate rather than insert — invalidation goes through the existing
      // filtering/sorting pipeline so it can't introduce duplicates regardless
      // of which category/tag is currently selected.
      void qc.invalidateQueries({ queryKey: ["community", "posts"] });
      void qc.invalidateQueries({ queryKey: ["community", "trending"] });
    };

    const slugOfSelected = selectedCategoryId
      ? categories.find((c) => c.id === selectedCategoryId)?.slug
      : undefined;

    hub.onPostCreated(handlePostCreated);
    hub
      .start()
      .then(() => {
        if (cancelled || !slugOfSelected) return undefined;
        return hub.joinCategory(slugOfSelected);
      })
      .catch(() => {
        /* connection error — UI keeps working via polling/query invalidation */
      });

    return () => {
      cancelled = true;
      hub.offPostCreated(handlePostCreated);
      // leaveCategory + stop are best-effort — connection may already be closed
      if (slugOfSelected) {
        hub.leaveCategory(slugOfSelected).catch(() => {});
      }
      hub.stop().catch(() => {});
    };
  }, [selectedCategoryId, categories, qc]);

  return (
    <div className="max-w-7xl mx-auto px-4 py-8 space-y-6">
      {/* Page header */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t("feed.title")}</h1>
          <p className="mt-2 max-w-xl text-text-secondary">
            {t("feed.subtitle")}
          </p>
        </div>
        <div className="flex gap-2">
          {isStudent && (
            <button
              type="button"
              onClick={() => setAskOpen(true)}
              className="btn btn-primary"
            >
              <Plus size={16} />
              <span>{t("feed.askQuestion")}</span>
            </button>
          )}
        </div>
      </div>

      <div className="flex flex-col lg:flex-row gap-8">
        {/* Sidebar */}
        <aside className="w-full lg:w-64 flex-shrink-0">
          <div className="sticky top-24 space-y-4">
            {isStudent && (
              <div className="card-premium overflow-hidden">
                <nav className="p-2 space-y-0.5">
                  <button
                    onClick={() => {
                      setTab("all");
                      setSelectedTag(undefined);
                    }}
                    className={`w-full text-start px-3 py-2 rounded-lg text-sm transition-colors ${
                      tab === "all"
                        ? "bg-brand-50 text-brand-700 font-semibold"
                        : "hover:bg-bg-subtle text-text-primary"
                    }`}
                  >
                    {t("feed.tabAll")}
                  </button>
                  <button
                    onClick={() => {
                      setTab("bookmarks");
                      setSelectedTag(undefined);
                    }}
                    className={`w-full text-start px-3 py-2 rounded-lg text-sm transition-colors flex items-center gap-2 ${
                      tab === "bookmarks"
                        ? "bg-brand-50 text-brand-700 font-semibold"
                        : "hover:bg-bg-subtle text-text-primary"
                    }`}
                  >
                    <Bookmark size={12} aria-hidden />
                    {t("feed.tabBookmarks")}
                  </button>
                </nav>
              </div>
            )}

            <div className="card-premium overflow-hidden">
              <div className="px-4 py-3 border-b border-border-subtle bg-bg-subtle/50">
                <h3 className="text-xs font-bold uppercase tracking-wider text-text-tertiary flex items-center gap-2">
                  <Filter size={12} aria-hidden />
                  {t("feed.categoriesTitle")}
                </h3>
              </div>
              <nav className="p-2 space-y-0.5">
                <button
                  onClick={() => {
                    setSelectedCategoryId(undefined);
                    setSelectedTag(undefined);
                    setTab("all");
                  }}
                  className={`group relative w-full text-start px-3 py-2 rounded-lg text-sm transition-colors flex items-center justify-between ${
                    !selectedCategoryId && tab === "all"
                      ? "bg-brand-50 text-brand-700 font-semibold"
                      : "hover:bg-bg-subtle text-text-primary"
                  }`}
                >
                  <span className="flex items-center gap-2">
                    <Hash size={12} className="opacity-60" aria-hidden />
                    {t("feed.allCategories")}
                  </span>
                  {!selectedCategoryId && tab === "all" && (
                    <ChevronRight size={14} className="rtl:rotate-180" />
                  )}
                </button>
                {categories.map((cat) => {
                  const isActive = selectedCategoryId === cat.id && tab === "all";
                  return (
                    <button
                      key={cat.id}
                      onClick={() => {
                        setSelectedCategoryId(cat.id);
                        setSelectedTag(undefined);
                        setTab("all");
                      }}
                      className={`group relative w-full text-start px-3 py-2 rounded-lg text-sm transition-colors flex items-center justify-between ${
                        isActive
                          ? "bg-brand-50 text-brand-700 font-semibold"
                          : "hover:bg-bg-subtle text-text-primary"
                      }`}
                    >
                      <span className="flex items-center gap-2 min-w-0 truncate">
                        <Hash size={12} className="opacity-60 shrink-0" aria-hidden />
                        <span className="truncate">{isRtl ? cat.nameAr : cat.nameEn}</span>
                      </span>
                      {isActive && <ChevronRight size={14} className="rtl:rotate-180 shrink-0" />}
                    </button>
                  );
                })}
              </nav>
            </div>
          </div>
        </aside>

        {/* Main Feed */}
        <main className="flex-1 space-y-5 min-w-0">
          {selectedTag && (
            <div className="flex items-center gap-2 rounded-lg border border-border-subtle bg-bg-subtle/40 px-3 py-2 text-sm">
              <span className="text-text-secondary">{t("feed.filteringByTag")}</span>
              <span className="inline-flex items-center gap-1 rounded-full bg-brand-50 px-2 py-0.5 text-xs font-semibold text-brand-700">
                #{selectedTag}
              </span>
              <button
                type="button"
                onClick={() => setSelectedTag(undefined)}
                className="ms-auto text-xs font-semibold text-brand-600 hover:underline"
              >
                {t("feed.clearTag")}
              </button>
            </div>
          )}

          {/* Sort + Search */}
          {tab === "all" && (
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
              <div className="inline-flex items-center gap-1 bg-bg-elevated p-1 rounded-xl border border-border-subtle shadow-elevation-1">
                <button
                  onClick={() => setSortBy("Newest")}
                  className={`flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-sm font-semibold transition-all ${
                    sortBy === "Newest"
                      ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm"
                      : "text-text-secondary hover:text-text-primary hover:bg-bg-subtle"
                  }`}
                >
                  <Clock size={14} />
                  {t("feed.sortNewest")}
                </button>
                <button
                  onClick={() => setSortBy("Trending")}
                  className={`flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-sm font-semibold transition-all ${
                    sortBy === "Trending"
                      ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm"
                      : "text-text-secondary hover:text-text-primary hover:bg-bg-subtle"
                  }`}
                >
                  <TrendingUp size={14} />
                  {t("feed.sortTrending")}
                </button>
              </div>

              <form onSubmit={handleSearch} className="relative w-full md:w-72">
                <Search className="absolute start-3 top-1/2 -translate-y-1/2 text-text-tertiary" size={16} aria-hidden />
                <input
                  type="text"
                  placeholder={t("feed.searchPlaceholder")}
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="input-premium ps-10"
                />
              </form>
            </div>
          )}

          {/* Trending strip */}
          {showTrending && (
            <section
              aria-label={t("feed.trendingTitle")}
              className="card-premium overflow-hidden"
            >
              <div className="flex items-center gap-2 border-b border-border-subtle bg-gradient-to-r from-warning-50/60 to-bg-elevated px-4 py-2.5">
                <span className="inline-flex size-6 items-center justify-center rounded-md bg-gradient-to-br from-warning-400 to-warning-600 text-white">
                  <Flame size={12} aria-hidden />
                </span>
                <h3 className="text-xs font-bold uppercase tracking-wider text-text-primary">
                  {t("feed.trendingTitle")}
                </h3>
                <span className="text-xs text-text-tertiary">·</span>
                <span className="text-xs text-text-tertiary">{t("feed.trendingSubtitle")}</span>
              </div>
              <ol className="divide-y divide-border-subtle">
                {trendingPosts.map((post, idx) => {
                  const score = post.upvoteCount - post.downvoteCount;
                  const categoryName =
                    categories.find((c) => c.id === post.categoryId)?.[isRtl ? "nameAr" : "nameEn"] ||
                    t("feed.generalCategory");
                  return (
                    <li key={post.id}>
                      <Link
                        to={`/student/community/${post.id}`}
                        className="group flex items-center gap-3 px-4 py-3 transition-colors hover:bg-bg-subtle/60"
                      >
                        <span
                          aria-hidden
                          className="inline-flex size-7 shrink-0 items-center justify-center rounded-lg bg-bg-subtle text-xs font-bold tabular-nums text-text-secondary group-hover:bg-brand-50 group-hover:text-brand-600"
                        >
                          {idx + 1}
                        </span>
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-semibold text-text-primary group-hover:text-brand-600">
                            {forumPostTitle(post, isRtl)}
                          </p>
                          <p className="mt-0.5 flex items-center gap-2 text-[11px] text-text-tertiary">
                            <span className="inline-flex items-center gap-1">
                              <Hash size={10} aria-hidden />
                              {categoryName}
                            </span>
                            <span aria-hidden>·</span>
                            <span className="inline-flex items-center gap-1">
                              <MessageSquare size={10} aria-hidden />
                              {t("feed.repliesCount", { count: post.replyCount })}
                            </span>
                          </p>
                        </div>
                        <div className="shrink-0 inline-flex items-center gap-1 rounded-full bg-brand-50 px-2 py-0.5 text-xs font-bold tabular-nums text-brand-700">
                          <ArrowUp size={11} strokeWidth={2.5} aria-hidden />
                          {score}
                        </div>
                      </Link>
                    </li>
                  );
                })}
              </ol>
            </section>
          )}

          {/* Post List */}
          <div className="space-y-3">
            {loading ? (
              Array(5).fill(0).map((_, i) => (
                <div key={i} className="h-32 skeleton rounded-2xl" />
              ))
            ) : posts.length > 0 ? (
              posts.map((post, idx) => {
                const score = post.upvoteCount - post.downvoteCount;
                const categoryName =
                  categories.find((c) => c.id === post.categoryId)?.[isRtl ? "nameAr" : "nameEn"] ||
                  t("feed.generalCategory");
                return (
                  <motion.div
                    key={post.id}
                    initial={{ opacity: 0, y: 6 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.24, delay: Math.min(idx * 0.04, 0.2) }}
                  >
                    <Link
                      to={`/student/community/${post.id}`}
                      className="block group card-premium p-5 sm:p-6 hover:border-brand-200"
                    >
                      <div className="flex gap-4">
                        {/* Vote Side */}
                        {isStudent && (
                          <div className="hidden sm:flex flex-col items-center gap-0.5 bg-bg-subtle rounded-xl px-1.5 py-2 h-fit border border-border-subtle">
                            <button
                              type="button"
                              aria-label={t("thread.upvote")}
                              onClick={(e) => handleVote(e, post.id, "Up")}
                              className="p-1.5 rounded-md text-text-tertiary hover:text-brand-600 hover:bg-brand-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent disabled:hover:text-text-tertiary"
                            >
                              <ArrowUp size={18} aria-hidden strokeWidth={2.5} />
                            </button>
                            <span className={`text-sm font-bold tabular-nums ${
                              score > 0 ? "text-brand-600" : score < 0 ? "text-danger-500" : "text-text-secondary"
                            }`}>
                              {score}
                            </span>
                            <button
                              type="button"
                              aria-label={t("thread.downvote")}
                              onClick={(e) => handleVote(e, post.id, "Down")}
                              className="p-1.5 rounded-md text-text-tertiary hover:text-danger-500 hover:bg-danger-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent disabled:hover:text-text-tertiary"
                            >
                              <ArrowDown size={18} aria-hidden strokeWidth={2.5} />
                            </button>
                          </div>
                        )}

                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 mb-2 flex-wrap">
                            <span className="badge badge-brand">
                              <Hash size={10} aria-hidden />
                              {categoryName}
                            </span>
                            <span className="text-xs text-text-tertiary">·</span>
                            <span className="text-xs text-text-tertiary">
                              {formatDistanceToNow(new Date(post.createdAt), { addSuffix: true, locale: dateLocale })}
                            </span>
                          </div>
                          <h3 className="text-lg font-bold mb-2 group-hover:text-brand-600 transition-colors tracking-tight leading-snug">
                            {forumPostTitle(post, isRtl)}
                          </h3>
                          <p className="text-text-secondary text-sm line-clamp-2 mb-3 leading-relaxed">
                            {forumPostBody(post, isRtl)}
                          </p>
                          {(post.tags ?? []).length > 0 && (
                            <div className="mb-3 flex flex-wrap gap-1.5">
                              {(post.tags ?? []).map((tag) => (
                                <button
                                  key={tag}
                                  type="button"
                                  onClick={(e) => handleTagClick(e, tag)}
                                  className="inline-flex items-center gap-0.5 rounded-full bg-brand-50/70 px-2 py-0.5 text-[11px] font-medium text-brand-700 transition-colors hover:bg-brand-100"
                                >
                                  #{tag}
                                </button>
                              ))}
                            </div>
                          )}
                          <div className="flex items-center gap-4 flex-wrap">
                            <div className="flex items-center gap-1.5 text-text-tertiary text-xs font-medium">
                              <MessageSquare size={13} aria-hidden />
                              <span>{t("feed.repliesCount", { count: post.replyCount })}</span>
                            </div>
                            <div className="flex items-center gap-1.5 text-text-tertiary text-xs">
                              <UserAvatar
                                userId={post.authorId}
                                name={post.authorName}
                                className="w-5 h-5 border border-white"
                                initialsClassName="text-[10px]"
                              />
                              <span className="font-medium">{post.authorName}</span>
                            </div>
                            {isStudent && (
                              <button
                                type="button"
                                onClick={(e) => handleBookmark(e, post.id)}
                                aria-label={
                                  post.isBookmarked ? t("actions.bookmarkRemove") : t("actions.bookmarkAdd")
                                }
                                title={
                                  post.isBookmarked ? t("actions.bookmarkRemove") : t("actions.bookmarkAdd")
                                }
                                className={`inline-flex items-center gap-1 rounded-md px-1.5 py-1 text-xs transition-colors ${
                                  post.isBookmarked
                                    ? "text-brand-600 hover:text-brand-700"
                                    : "text-text-tertiary hover:text-brand-600"
                                }`}
                              >
                                <Bookmark
                                  size={14}
                                  aria-hidden
                                  fill={post.isBookmarked ? "currentColor" : "none"}
                                />
                              </button>
                            )}
                            {/* Mobile vote score chip */}
                            {isStudent && (
                              <div className="sm:hidden ml-auto flex items-center gap-1 text-xs font-bold tabular-nums">
                                <ArrowUp size={12} className="text-text-tertiary" />
                                <span className={score > 0 ? "text-brand-600" : score < 0 ? "text-danger-500" : "text-text-secondary"}>{score}</span>
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    </Link>
                  </motion.div>
                );
              })
            ) : (
              (() => {
                const activeCategory = selectedCategoryId
                  ? categories.find((c) => c.id === selectedCategoryId)
                  : undefined;
                const categoryName = activeCategory
                  ? (isRtl ? activeCategory.nameAr : activeCategory.nameEn)
                  : undefined;
                const isSearching = searchQuery.trim().length > 0;
                const isBookmarksTab = tab === "bookmarks";
                const title = isBookmarksTab
                  ? t("feed.emptyBookmarksTitle")
                  : isSearching
                    ? t("feed.emptySearchTitle")
                    : categoryName
                      ? t("feed.emptyCategoryTitle", { category: categoryName })
                      : t("feed.emptyTitle");
                const description = isBookmarksTab
                  ? t("feed.emptyBookmarksBody")
                  : isSearching
                    ? t("feed.emptySearchBody", { query: searchQuery.trim() })
                    : categoryName
                      ? t("feed.emptyCategoryBody", { category: categoryName })
                      : t("feed.emptyBody");
                return (
                  <EmptyState
                    icon={MessageSquare}
                    title={title}
                    description={description}
                    action={
                      isStudent && !isBookmarksTab
                        ? {
                            label: t("feed.askQuestion"),
                            onClick: () => setAskOpen(true),
                            leadingIcon: <Plus size={14} />,
                          }
                        : undefined
                    }
                  />
                );
              })()
            )}
          </div>
        </main>
      </div>

      {isStudent && (
        <AskQuestionModal
          isOpen={askOpen}
          onOpenChange={setAskOpen}
          categories={categories}
        />
      )}
    </div>
  );
}
