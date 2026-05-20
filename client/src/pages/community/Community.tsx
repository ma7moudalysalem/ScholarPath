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
  ChevronRight
} from "lucide-react";
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
    <div className="flex flex-col lg:flex-row gap-8 max-w-7xl mx-auto px-4 py-8">
      {/* Sidebar */}
      <aside className="w-full lg:w-64 flex-shrink-0">
        <div className="sticky top-24 space-y-6">
          <button
            type="button"
            onClick={() => setAskOpen(true)}
            className="w-full cta-pill bg-brand-500 text-white flex items-center justify-center gap-2 shadow-lg hover:bg-brand-600 transition-all"
          >
            <Plus size={18} />
            <span>{t("feed.askQuestion")}</span>
          </button>

          <div className="bg-bg-elevated rounded-xl border border-border-subtle overflow-hidden shadow-sm">
            <div className="px-4 py-3 border-b border-border-subtle bg-bg-muted">
              <h3 className="text-sm font-semibold flex items-center gap-2">
                <Filter size={14} className="text-brand-500" />
                {t("feed.categoriesTitle")}
              </h3>
            </div>
            <nav className="p-2">
              <button
                onClick={() => setSelectedCategoryId(undefined)}
                className={`w-full text-start px-3 py-2 rounded-lg text-sm transition-colors flex items-center justify-between ${
                  !selectedCategoryId ? "bg-brand-50 text-brand-600 font-medium" : "hover:bg-bg-subtle"
                }`}
              >
                <span>{t("feed.allCategories")}</span>
                {!selectedCategoryId && <ChevronRight size={14} />}
              </button>
              {categories.map((cat) => (
                <button
                  key={cat.id}
                  onClick={() => setSelectedCategoryId(cat.id)}
                  className={`w-full text-start px-3 py-2 rounded-lg text-sm transition-colors flex items-center justify-between ${
                    selectedCategoryId === cat.id ? "bg-brand-50 text-brand-600 font-medium" : "hover:bg-bg-subtle"
                  }`}
                >
                  <span>{isRtl ? cat.nameAr : cat.nameEn}</span>
                  {selectedCategoryId === cat.id && <ChevronRight size={14} />}
                </button>
              ))}
            </nav>
          </div>
        </div>
      </aside>

      {/* Main Feed */}
      <main className="flex-1 space-y-6">
        {/* Header / Search */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-4 bg-bg-elevated p-1 rounded-xl border border-border-subtle shadow-sm w-full md:w-auto">
            <button
              onClick={() => setSortBy("Newest")}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                sortBy === "Newest" ? "bg-white shadow-sm text-brand-600" : "text-text-secondary hover:bg-bg-subtle"
              }`}
            >
              <Clock size={16} />
              {t("feed.sortNewest")}
            </button>
            <button
              onClick={() => setSortBy("MostVoted")}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                sortBy === "MostVoted" ? "bg-white shadow-sm text-brand-600" : "text-text-secondary hover:bg-bg-subtle"
              }`}
            >
              <TrendingUp size={16} />
              {t("feed.sortTrending")}
            </button>
          </div>

          <form onSubmit={handleSearch} className="relative w-full md:w-72">
            <Search className="absolute start-3 top-1/2 -translate-y-1/2 text-text-tertiary" size={18} />
            <input
              type="text"
              placeholder={t("feed.searchPlaceholder")}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full ps-10 pe-4 py-2 rounded-xl border border-border-subtle bg-bg-elevated focus:ring-2 focus:ring-brand-400 focus:border-transparent outline-none transition-all text-sm shadow-sm"
            />
          </form>
        </div>

        {/* Post List */}
        <div className="space-y-4">
          {loading ? (
            Array(5).fill(0).map((_, i) => (
              <div key={i} className="h-32 bg-bg-elevated rounded-2xl border border-border-subtle animate-pulse" />
            ))
          ) : posts.length > 0 ? (
            posts.map((post) => {
              const isOwnPost = post.authorId === currentUserId;
              const voteDisabledTitle = isOwnPost ? t("actions.voteOwnPost") : undefined;
              return (
              <Link
                key={post.id}
                to={`/student/community/${post.id}`}
                className="block group bg-bg-elevated p-6 rounded-2xl border border-border-subtle shadow-sm hover:shadow-md transition-all hover:border-brand-200"
              >
                <div className="flex gap-4">
                  {/* Vote Side — disabled (visually + behaviourally) on the user's
                      own post, matching the server-side "cannot vote on your own"
                      rule so they never see a 409 toast. */}
                  <div className="hidden sm:flex flex-col items-center gap-1 bg-bg-subtle rounded-xl p-2 h-fit">
                    <button
                      type="button"
                      aria-label="Upvote"
                      disabled={isOwnPost}
                      title={voteDisabledTitle}
                      onClick={(e) => handleVote(e, post.id, "Up")}
                      className="text-text-tertiary hover:text-brand-500 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:text-text-tertiary"
                    >
                      <ArrowUp size={20} aria-hidden />
                    </button>
                    <span className="text-sm font-bold text-text-secondary">
                      {post.upvoteCount - post.downvoteCount}
                    </span>
                    <button
                      type="button"
                      aria-label="Downvote"
                      disabled={isOwnPost}
                      title={voteDisabledTitle}
                      onClick={(e) => handleVote(e, post.id, "Down")}
                      className="text-text-tertiary hover:text-danger-500 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:text-text-tertiary"
                    >
                      <ArrowDown size={20} aria-hidden />
                    </button>
                  </div>

                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-xs font-medium text-brand-600 bg-brand-50 px-2 py-0.5 rounded-full">
                        {categories.find(c => c.id === post.categoryId)?.[isRtl ? 'nameAr' : 'nameEn'] || t("feed.generalCategory")}
                      </span>
                      <span className="text-xs text-text-tertiary">•</span>
                      <span className="text-xs text-text-tertiary">
                        {formatDistanceToNow(new Date(post.createdAt), { addSuffix: true, locale: dateLocale })}
                      </span>
                    </div>
                    <h3 className="text-lg font-bold mb-2 group-hover:text-brand-500 transition-colors">
                      {post.title}
                    </h3>
                    <p className="text-text-secondary text-sm line-clamp-2 mb-4">
                      {post.bodyMarkdown}
                    </p>
                    <div className="flex items-center gap-4">
                      <div className="flex items-center gap-1 text-text-tertiary text-xs">
                        <MessageSquare size={14} />
                        <span>{t("feed.repliesCount", { count: post.replyCount })}</span>
                      </div>
                      <div className="flex items-center gap-1 text-text-tertiary text-xs">
                        <UserAvatar
                          userId={post.authorId}
                          name={post.authorName}
                          className="w-5 h-5 border border-white"
                          initialsClassName="text-[10px]"
                        />
                        <span>{post.authorName}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </Link>
              );
            })
          ) : (
            <div className="text-center py-20 bg-bg-elevated rounded-3xl border border-dashed border-border-default">
              <div className="bg-bg-subtle w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                <MessageSquare className="text-text-tertiary" size={32} />
              </div>
              <h3 className="text-xl font-bold mb-2">{t("feed.emptyTitle")}</h3>
              <p className="text-text-secondary">{t("feed.emptyBody")}</p>
            </div>
          )}
        </div>
      </main>

      <AskQuestionModal
        isOpen={askOpen}
        onOpenChange={setAskOpen}
        categories={categories}
      />
    </div>
  );
}
