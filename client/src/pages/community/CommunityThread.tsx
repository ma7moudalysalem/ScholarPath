import { useState } from "react";
import { useParams, Link } from "react-router";
import { useTranslation } from "react-i18next";
import {
  ArrowLeft,
  ArrowUp,
  ArrowDown,
  MessageSquare,
  Flag,
  Send,
  Shield,
  Loader2,
} from "lucide-react";
import { motion } from "motion/react";
import { communityApi, type VoteType } from "@/services/api/community";
import { toast } from "sonner";
import { UserAvatar } from "@/components/common/UserAvatar";
import { formatDistanceToNow } from "date-fns";
import { ar } from "date-fns/locale";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/stores/authStore";
import { apiErrorMessage } from "@/services/api/client";

export function CommunityThread() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation("community");
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isRtl ? ar : undefined;
  const qc = useQueryClient();
  // The server returns 409 on self-vote; we pre-empt by disabling the buttons
  // when the author is the current user.
  const currentUserId = useAuthStore((state) => state.user?.id);

  const [replyBody, setReplyBody] = useState("");

  const { data: thread, isLoading: loading } = useQuery({
    queryKey: ["community", "thread", id],
    queryFn: () => communityApi.getPostDetails(id!),
    enabled: !!id,
  });

  const voteMutation = useMutation({
    mutationFn: ({ postId, type }: { postId: string; type: VoteType }) =>
      communityApi.toggleVote(postId, type),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["community", "thread", id] });
    },
    // Surface the server's own message (e.g. "You cannot vote on your own
    // post.") instead of a generic fallback.
    onError: (err) => {
      toast.error(apiErrorMessage(err, t("actions.voteError")));
    },
  });

  const replyMutation = useMutation({
    mutationFn: (body: string) => communityApi.createReply(id!, { bodyMarkdown: body }),
    onSuccess: () => {
      setReplyBody("");
      void qc.invalidateQueries({ queryKey: ["community", "thread", id] });
    },
    onError: () => {
      toast.error(t("actions.replyError"));
    },
  });

  const handleVote = (postId: string, type: VoteType) => {
    voteMutation.mutate({ postId, type });
  };

  const handleReply = (e: React.FormEvent) => {
    e.preventDefault();
    if (!id || !replyBody.trim()) return;
    replyMutation.mutate(replyBody);
  };

  const handleFlag = async (postId: string) => {
    const reason = prompt(t("actions.flagReasonPrompt"));
    if (!reason) return;

    try {
      await communityApi.flagPost(postId, { reason });
      alert(t("actions.flagSuccess"));
    } catch (error) {
      console.error("Failed to flag", error);
    }
  };

  if (loading) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-12 space-y-8 animate-pulse">
        <div className="h-8 w-32 bg-bg-subtle rounded" />
        <div className="h-64 bg-bg-elevated rounded-3xl border border-border-subtle" />
      </div>
    );
  }

  if (!thread) {
    return (
      <div className="text-center py-20">
        <h2 className="text-2xl font-bold">{t("thread.notFound")}</h2>
        <Link to="/student/community" className="text-brand-500 hover:underline mt-4 block">
          {t("thread.backToFeed")}
        </Link>
      </div>
    );
  }

  const postScore = thread.post.upvoteCount - thread.post.downvoteCount;

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {/* Back button */}
      <Link
        to="/student/community"
        className="inline-flex items-center gap-2 text-text-secondary hover:text-text-primary transition-colors mb-6 group"
      >
        <ArrowLeft size={16} className="group-hover:-translate-x-1 transition-transform rtl:rotate-180 rtl:group-hover:translate-x-1" aria-hidden />
        <span className="font-semibold text-sm">{t("thread.backToFeed")}</span>
      </Link>

      {/* Main Post */}
      <motion.div
        initial={{ opacity: 0, y: 6 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.28 }}
        className="card-premium overflow-hidden mb-6"
      >
        <div className="p-6 sm:p-8">
          <div className="flex gap-5 sm:gap-6">
            {/* Vote Side — disabled on your own post (matches the server-side
                "cannot vote on your own post" rule). */}
            {(() => {
              const isOwnPost = thread.post.authorId === currentUserId;
              const title = isOwnPost ? t("actions.voteOwnPost") : undefined;
              return (
                <div className="flex flex-col items-center gap-0.5 bg-bg-subtle/70 rounded-xl px-1.5 py-2 h-fit border border-border-subtle">
                  <button
                    type="button"
                    onClick={() => handleVote(thread.post.id, "Up")}
                    disabled={isOwnPost}
                    title={title}
                    aria-label={t("thread.upvote")}
                    className="p-1.5 rounded-md text-text-tertiary hover:text-brand-600 hover:bg-brand-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent disabled:hover:text-text-tertiary"
                  >
                    <ArrowUp size={20} strokeWidth={2.5} aria-hidden />
                  </button>
                  <span className={`font-bold text-base tabular-nums ${
                    postScore > 0 ? "text-brand-600" : postScore < 0 ? "text-danger-500" : "text-text-secondary"
                  }`}>
                    {postScore}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleVote(thread.post.id, "Down")}
                    disabled={isOwnPost}
                    title={title}
                    aria-label={t("thread.downvote")}
                    className="p-1.5 rounded-md text-text-tertiary hover:text-danger-500 hover:bg-danger-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent disabled:hover:text-text-tertiary"
                  >
                    <ArrowDown size={20} strokeWidth={2.5} aria-hidden />
                  </button>
                </div>
              );
            })()}

            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between mb-5 gap-3">
                <div className="flex items-center gap-3 min-w-0">
                  <UserAvatar
                    userId={thread.post.authorId}
                    name={thread.post.authorName}
                    className="w-10 h-10 ring-2 ring-bg-elevated"
                  />
                  <div className="min-w-0">
                    <h4 className="text-sm font-bold tracking-tight truncate">{thread.post.authorName}</h4>
                    <span className="text-xs text-text-tertiary">
                      {formatDistanceToNow(new Date(thread.post.createdAt), { addSuffix: true, locale: dateLocale })}
                    </span>
                  </div>
                </div>
                <button
                  onClick={() => handleFlag(thread.post.id)}
                  aria-label={t("actions.flagReasonPrompt")}
                  className="p-2 text-text-tertiary hover:text-danger-500 hover:bg-danger-50 rounded-lg transition-colors shrink-0"
                >
                  <Flag size={16} aria-hidden />
                </button>
              </div>

              <h1 className="text-2xl sm:text-3xl font-bold mb-5 leading-tight tracking-tight">
                {thread.post.title}
              </h1>

              <div className="max-w-none text-text-secondary leading-relaxed mb-6 space-y-2 text-[15px]">
                {thread.post.bodyMarkdown.split('\n').filter((l) => l.trim()).map((line, idx) => (
                  <p key={idx}>{line}</p>
                ))}
              </div>

              <div className="flex items-center gap-5 border-t border-border-subtle pt-5 flex-wrap">
                <div className="flex items-center gap-1.5 text-text-secondary text-sm font-medium">
                  <MessageSquare size={16} aria-hidden />
                  <span>{t("thread.commentsCount", { count: thread.post.replyCount })}</span>
                </div>
                <div className="flex items-center gap-1.5 text-success-600 text-sm">
                  <Shield size={16} aria-hidden />
                  <span className="font-medium">{t("thread.verifiedSafe")}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </motion.div>

      {/* Reply Form */}
      <div className="mb-10">
        <form onSubmit={handleReply} className="relative">
          <textarea
            value={replyBody}
            onChange={(e) => setReplyBody(e.target.value)}
            placeholder={t("thread.replyPlaceholder")}
            className="w-full bg-bg-elevated border border-border-default rounded-2xl px-5 py-4 pe-16 outline-none focus:ring-2 focus:ring-brand-400/40 focus:border-brand-400 transition-all shadow-elevation-1 min-h-[120px] resize-none text-sm leading-relaxed placeholder:text-text-tertiary"
          />
          <button
            type="submit"
            disabled={replyMutation.isPending || !replyBody.trim()}
            aria-label={t("ask.submit")}
            className="absolute end-3 bottom-3 flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm transition-all hover:shadow-brand-md hover:from-brand-600 hover:to-brand-800 active:scale-95 disabled:bg-none disabled:bg-bg-subtle disabled:text-text-tertiary disabled:shadow-none disabled:cursor-not-allowed"
          >
            {replyMutation.isPending ? (
              <Loader2 size={18} className="animate-spin" aria-hidden />
            ) : (
              <Send size={16} className="rtl:rotate-180" aria-hidden />
            )}
          </button>
        </form>
      </div>

      {/* Replies List */}
      <div className="space-y-3 relative">
        {thread.replies.length > 0 && (
          <h2 className="text-sm font-bold text-text-secondary uppercase tracking-wider mb-2">
            {t("thread.commentsCount", { count: thread.post.replyCount })}
          </h2>
        )}
        {thread.replies.map((reply, idx) => {
          const isOwnReply = reply.authorId === currentUserId;
          const replyVoteTitle = isOwnReply ? t("actions.voteOwnPost") : undefined;
          const replyScore = reply.upvoteCount - reply.downvoteCount;
          return (
            <motion.div
              key={reply.id}
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.24, delay: Math.min(idx * 0.04, 0.2) }}
              className="relative ps-6 sm:ps-8"
            >
              {/* Thread connector */}
              <span aria-hidden className="absolute start-2 sm:start-3 top-0 bottom-0 w-px bg-border-subtle" />
              <span aria-hidden className="absolute start-2 sm:start-3 top-6 w-3 sm:w-5 h-px bg-border-subtle" />

              <div className="card-premium p-5">
                <div className="flex items-start justify-between mb-3 gap-3">
                  <div className="flex items-center gap-2.5 min-w-0">
                    <UserAvatar
                      userId={reply.authorId}
                      name={reply.authorName}
                      className="w-8 h-8 ring-2 ring-bg-elevated"
                      initialsClassName="text-[10px]"
                    />
                    <div className="min-w-0">
                      <span className="text-sm font-bold tracking-tight block truncate">{reply.authorName}</span>
                      <span className="text-xs text-text-tertiary">
                        {formatDistanceToNow(new Date(reply.createdAt), { addSuffix: true, locale: dateLocale })}
                      </span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      type="button"
                      onClick={() => handleVote(reply.id, "Up")}
                      disabled={isOwnReply}
                      title={replyVoteTitle}
                      aria-label={t("thread.upvoteReply")}
                      className="p-1.5 rounded-md text-text-tertiary hover:text-brand-600 hover:bg-brand-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent disabled:hover:text-text-tertiary"
                    >
                      <ArrowUp size={14} strokeWidth={2.5} aria-hidden />
                    </button>
                    <span className={`text-xs font-bold w-5 text-center tabular-nums ${
                      replyScore > 0 ? "text-brand-600" : replyScore < 0 ? "text-danger-500" : "text-text-secondary"
                    }`}>
                      {replyScore}
                    </span>
                    <button
                      type="button"
                      onClick={() => handleFlag(reply.id)}
                      aria-label={t("actions.flagReasonPrompt")}
                      className="p-1.5 rounded-md text-text-tertiary hover:text-danger-500 hover:bg-danger-50 transition-colors"
                    >
                      <Flag size={12} aria-hidden />
                    </button>
                  </div>
                </div>
                <div className="text-text-secondary text-sm leading-relaxed space-y-1.5">
                  {reply.bodyMarkdown.split('\n').filter((l) => l.trim()).map((line, idx) => (
                    <p key={idx}>{line}</p>
                  ))}
                </div>
              </div>
            </motion.div>
          );
        })}
      </div>
    </div>
  );
}
