import { useState } from "react";
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
} from "lucide-react";
import { motion } from "motion/react";
import { communityApi, type ForumCategory, type VoteType } from "@/services/api/community";
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

export function Community() {
  const { t, i18n } = useTranslation("community");
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isRtl ? ar : undefined;

  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>();
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState("Newest");
  const [askOpen, setAskOpen] = useState(false);

  // The server returns 409 if someone tries to vote on a post they authored.
  // We pre-empt that here by disabling the buttons for posts authored by the
  // current user — single source of truth is the auth store.
  const currentUserId = useAuthStore((state) => state.user?.id);

  const { data: categories = [] } = useQuery<ForumCategory[]>({
    queryKey: ["community", "categories"],
    queryFn: () => communityApi.getCategories(),
  });

  const { data: postsData, isLoading: loading } = useQuery({
    queryKey: ["community", "posts", selectedCategoryId, searchQuery, sortBy],
    queryFn: () => communityApi.getPosts({
      categoryId: selectedCategoryId,
      query: searchQuery,
      sortBy,
      page: 1,
      pageSize: 20
    }),
    placeholderData: keepPreviousData,
  });

  const posts = postsData?.items ?? [];

  const qc = useQueryClient();
  const voteMutation = useMutation({
    mutationFn: ({ postId, type }: { postId: string; type: VoteType }) =>
      communityApi.toggleVote(postId, type),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["community", "posts"] }),
    // Surface the server's own message ("You cannot vote on your own post.",
    // "Voting on hidden posts is not allowed.", etc.) instead of a generic
    // fallback — the user has no way to act on a vague "could not vote".
    onError: (err) => toast.error(apiErrorMessage(err, t("actions.voteError"))),
  });

  // The vote buttons sit inside the post Link — block navigation when voting.
  const handleVote = (e: React.MouseEvent, postId: string, type: VoteType) => {
    e.preventDefault();
    e.stopPropagation();
    voteMutation.mutate({ postId, type });
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    // Search is handled by the query dependency on searchQuery
  };

  return (
    <div className="max-w-7xl mx-auto px-4 py-8 space-y-6">
      {/* Page header */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t("feed.askQuestion")}</h1>
          <p className="mt-2 max-w-xl text-text-secondary">
            {t("feed.emptyBody")}
          </p>
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setAskOpen(true)}
            className="btn btn-primary"
          >
            <Plus size={16} />
            <span>{t("feed.askQuestion")}</span>
          </button>
        </div>
      </div>

      <div className="flex flex-col lg:flex-row gap-8">
        {/* Sidebar */}
        <aside className="w-full lg:w-64 flex-shrink-0">
          <div className="sticky top-24 space-y-4">
            <div className="card-premium overflow-hidden">
              <div className="px-4 py-3 border-b border-border-subtle bg-bg-subtle/50">
                <h3 className="text-xs font-bold uppercase tracking-wider text-text-tertiary flex items-center gap-2">
                  <Filter size={12} aria-hidden />
                  {t("feed.categoriesTitle")}
                </h3>
              </div>
              <nav className="p-2 space-y-0.5">
                <button
                  onClick={() => setSelectedCategoryId(undefined)}
                  className={`group relative w-full text-start px-3 py-2 rounded-lg text-sm transition-colors flex items-center justify-between ${
                    !selectedCategoryId
                      ? "bg-brand-50 text-brand-700 font-semibold"
                      : "hover:bg-bg-subtle text-text-primary"
                  }`}
                >
                  {!selectedCategoryId && (
                    <span aria-hidden className="absolute start-0 top-1.5 bottom-1.5 w-[3px] rounded-full bg-brand-500" />
                  )}
                  <span className="flex items-center gap-2">
                    <Hash size={12} className="opacity-60" aria-hidden />
                    {t("feed.allCategories")}
                  </span>
                  {!selectedCategoryId && <ChevronRight size={14} className="rtl:rotate-180" />}
                </button>
                {categories.map((cat) => {
                  const isActive = selectedCategoryId === cat.id;
                  return (
                    <button
                      key={cat.id}
                      onClick={() => setSelectedCategoryId(cat.id)}
                      className={`group relative w-full text-start px-3 py-2 rounded-lg text-sm transition-colors flex items-center justify-between ${
                        isActive
                          ? "bg-brand-50 text-brand-700 font-semibold"
                          : "hover:bg-bg-subtle text-text-primary"
                      }`}
                    >
                      {isActive && (
                        <span aria-hidden className="absolute start-0 top-1.5 bottom-1.5 w-[3px] rounded-full bg-brand-500" />
                      )}
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
          {/* Sort + Search */}
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
                onClick={() => setSortBy("MostVoted")}
                className={`flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-sm font-semibold transition-all ${
                  sortBy === "MostVoted"
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

          {/* Post List */}
          <div className="space-y-3">
            {loading ? (
              Array(5).fill(0).map((_, i) => (
                <div key={i} className="h-32 skeleton rounded-2xl" />
              ))
            ) : posts.length > 0 ? (
              posts.map((post, idx) => {
                const isOwnPost = post.authorId === currentUserId;
                const voteDisabledTitle = isOwnPost ? t("actions.voteOwnPost") : undefined;
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
                        {/* Vote Side — disabled (visually + behaviourally) on the user's
                            own post, matching the server-side "cannot vote on your own"
                            rule so they never see a 409 toast. */}
                        <div className="hidden sm:flex flex-col items-center gap-0.5 bg-bg-subtle rounded-xl px-1.5 py-2 h-fit border border-border-subtle">
                          <button
                            type="button"
                            aria-label={t("thread.upvote")}
                            disabled={isOwnPost}
                            title={voteDisabledTitle}
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
                            disabled={isOwnPost}
                            title={voteDisabledTitle}
                            onClick={(e) => handleVote(e, post.id, "Down")}
                            className="p-1.5 rounded-md text-text-tertiary hover:text-danger-500 hover:bg-danger-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent disabled:hover:text-text-tertiary"
                          >
                            <ArrowDown size={18} aria-hidden strokeWidth={2.5} />
                          </button>
                        </div>

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
                            {post.title}
                          </h3>
                          <p className="text-text-secondary text-sm line-clamp-2 mb-4 leading-relaxed">
                            {post.bodyMarkdown}
                          </p>
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
                            {/* Mobile vote score chip */}
                            <div className="sm:hidden ml-auto flex items-center gap-1 text-xs font-bold tabular-nums">
                              <ArrowUp size={12} className="text-text-tertiary" />
                              <span className={score > 0 ? "text-brand-600" : score < 0 ? "text-danger-500" : "text-text-secondary"}>{score}</span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </Link>
                  </motion.div>
                );
              })
            ) : (
              <EmptyState
                icon={MessageSquare}
                title={t("feed.emptyTitle")}
                description={t("feed.emptyBody")}
                action={{
                  label: t("feed.askQuestion"),
                  onClick: () => setAskOpen(true),
                  leadingIcon: <Plus size={14} />,
                }}
              />
            )}
          </div>
        </main>
      </div>

      <AskQuestionModal
        isOpen={askOpen}
        onOpenChange={setAskOpen}
        categories={categories}
      />
    </div>
  );
}
