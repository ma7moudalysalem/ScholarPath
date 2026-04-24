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
import { communityApi, type ForumCategory } from "@/services/api/community";
import { Link } from "react-router";
import { formatDistanceToNow } from "date-fns";
import { ar } from "date-fns/locale";
import { useQuery, keepPreviousData } from "@tanstack/react-query";

export function Community() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isRtl ? ar : undefined;

  const [selectedCategoryId, setSelectedCategoryId] = useState<string | undefined>();
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState("Newest");

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
            className="w-full cta-pill bg-brand-500 text-white flex items-center justify-center gap-2 shadow-lg hover:bg-brand-600 transition-all"
          >
            <Plus size={18} />
            <span>{t("community.ask_question", "Ask a Question")}</span>
          </button>

          <div className="bg-bg-elevated rounded-xl border border-border-subtle overflow-hidden shadow-sm">
            <div className="px-4 py-3 border-b border-border-subtle bg-bg-muted">
              <h3 className="text-sm font-semibold flex items-center gap-2">
                <Filter size={14} className="text-brand-500" />
                {t("community.categories", "Categories")}
              </h3>
            </div>
            <nav className="p-2">
              <button
                onClick={() => setSelectedCategoryId(undefined)}
                className={`w-full text-start px-3 py-2 rounded-lg text-sm transition-colors flex items-center justify-between ${
                  !selectedCategoryId ? "bg-brand-50 text-brand-600 font-medium" : "hover:bg-bg-subtle"
                }`}
              >
                <span>{t("community.all_categories", "All Topics")}</span>
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
              {t("community.sort_newest", "Newest")}
            </button>
            <button
              onClick={() => setSortBy("MostVoted")}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                sortBy === "MostVoted" ? "bg-white shadow-sm text-brand-600" : "text-text-secondary hover:bg-bg-subtle"
              }`}
            >
              <TrendingUp size={16} />
              {t("community.sort_trending", "Trending")}
            </button>
          </div>

          <form onSubmit={handleSearch} className="relative w-full md:w-72">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary" size={18} />
            <input
              type="text"
              placeholder={t("community.search_placeholder", "Search discussions...")}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 rounded-xl border border-border-subtle bg-bg-elevated focus:ring-2 focus:ring-brand-400 focus:border-transparent outline-none transition-all text-sm shadow-sm"
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
            posts.map((post) => (
              <Link
                key={post.id}
                to={`/student/community/${post.id}`}
                className="block group bg-bg-elevated p-6 rounded-2xl border border-border-subtle shadow-sm hover:shadow-md transition-all hover:border-brand-200"
              >
                <div className="flex gap-4">
                  {/* Vote Side */}
                  <div className="hidden sm:flex flex-col items-center gap-1 bg-bg-subtle rounded-xl p-2 h-fit">
                    <button className="text-text-tertiary hover:text-brand-500 transition-colors">
                      <ArrowUp size={20} />
                    </button>
                    <span className="text-sm font-bold text-text-secondary">
                      {post.upvoteCount - post.downvoteCount}
                    </span>
                    <button className="text-text-tertiary hover:text-danger-500 transition-colors">
                      <ArrowDown size={20} />
                    </button>
                  </div>

                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-xs font-medium text-brand-600 bg-brand-50 px-2 py-0.5 rounded-full">
                        {categories.find(c => c.id === post.categoryId)?.[isRtl ? 'nameAr' : 'nameEn'] || t("community.general", "General")}
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
                        <span>{post.replyCount} {t("community.replies", "replies")}</span>
                      </div>
                      <div className="flex items-center gap-1 text-text-tertiary text-xs">
                        <div className="w-5 h-5 rounded-full bg-brand-100 flex items-center justify-center text-[10px] text-brand-600 font-bold border border-white">
                          {post.authorName[0]}
                        </div>
                        <span>{post.authorName}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </Link>
            ))
          ) : (
            <div className="text-center py-20 bg-bg-elevated rounded-3xl border border-dashed border-border-default">
              <div className="bg-bg-subtle w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-4">
                <MessageSquare className="text-text-tertiary" size={32} />
              </div>
              <h3 className="text-xl font-bold mb-2">{t("community.no_posts", "No discussions found")}</h3>
              <p className="text-text-secondary">{t("community.no_posts_desc", "Be the first to start a conversation in this category.")}</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
