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
  Shield
} from "lucide-react";
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

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {/* Back button */}
      <Link 
        to="/student/community" 
        className="inline-flex items-center gap-2 text-text-secondary hover:text-brand-500 transition-colors mb-6 group"
      >
        <ArrowLeft size={18} className="group-hover:-translate-x-1 transition-transform rtl:group-hover:translate-x-1" />
        <span className="font-medium text-sm">{t("thread.backToFeed")}</span>
      </Link>

      {/* Main Post */}
      <div className="bg-bg-elevated rounded-3xl border border-border-subtle shadow-sm overflow-hidden mb-8">
        <div className="p-8">
          <div className="flex gap-6">
            {/* Vote Side — disabled on your own post (matches the server-side
                "cannot vote on your own post" rule). */}
            {(() => {
              const isOwnPost = thread.post.authorId === currentUserId;
              const title = isOwnPost ? t("actions.voteOwnPost") : undefined;
              return (
                <div className="flex flex-col items-center gap-2 bg-bg-subtle rounded-2xl p-2 h-fit">
                  <button
                    type="button"
                    onClick={() => handleVote(thread.post.id, "Up")}
                    disabled={isOwnPost}
                    title={title}
                    aria-label="Upvote"
                    className="p-1 hover:text-brand-500 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:text-current"
                  >
                    <ArrowUp size={24} />
                  </button>
                  <span className="font-bold text-lg">{thread.post.upvoteCount - thread.post.downvoteCount}</span>
                  <button
                    type="button"
                    onClick={() => handleVote(thread.post.id, "Down")}
                    disabled={isOwnPost}
                    title={title}
                    aria-label="Downvote"
                    className="p-1 hover:text-danger-500 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:text-current"
                  >
                    <ArrowDown size={24} />
                  </button>
                </div>
              );
            })()}

            <div className="flex-1">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-3">
                  <UserAvatar
                    userId={thread.post.authorId}
                    name={thread.post.authorName}
                    className="w-10 h-10"
                  />
                  <div>
                    <h4 className="text-sm font-bold">{thread.post.authorName}</h4>
                    <span className="text-xs text-text-tertiary">
                      {formatDistanceToNow(new Date(thread.post.createdAt), { addSuffix: true, locale: dateLocale })}
                    </span>
                  </div>
                </div>
                <button 
                  onClick={() => handleFlag(thread.post.id)}
                  className="text-text-tertiary hover:text-danger-500 p-2 transition-colors"
                >
                  <Flag size={18} />
                </button>
              </div>

              <h1 className="text-2xl sm:text-3xl font-bold mb-6 leading-tight">
                {thread.post.title}
              </h1>
              
              <div className="prose prose-slate max-w-none text-text-secondary leading-relaxed mb-8">
                {thread.post.bodyMarkdown.split('\n').map((line, idx) => (
                  <p key={idx}>{line}</p>
                ))}
              </div>

              <div className="flex items-center gap-6 border-t border-border-subtle pt-6">
                <div className="flex items-center gap-2 text-text-tertiary text-sm">
                  <MessageSquare size={18} />
                  <span className="font-medium">{t("thread.commentsCount", { count: thread.post.replyCount })}</span>
                </div>
                <div className="flex items-center gap-2 text-text-tertiary text-sm">
                  <Shield size={18} />
                  <span>{t("thread.verifiedSafe")}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Reply Form */}
      <div className="mb-10">
        <form onSubmit={handleReply} className="relative">
          <textarea
            value={replyBody}
            onChange={(e) => setReplyBody(e.target.value)}
            placeholder={t("thread.replyPlaceholder")}
            className="w-full bg-bg-elevated border border-border-subtle rounded-2xl p-6 pe-16 outline-none focus:ring-2 focus:ring-brand-400 focus:border-transparent transition-all shadow-sm min-h-[120px] resize-none"
          />
          <button
            type="submit"
            disabled={replyMutation.isPending || !replyBody.trim()}
            className="absolute end-4 bottom-4 p-3 bg-brand-500 text-white rounded-xl shadow-lg hover:bg-brand-600 disabled:opacity-50 disabled:shadow-none transition-all"
          >
            <Send size={20} />
          </button>
        </form>
      </div>

      {/* Replies List */}
      <div className="space-y-6 relative before:absolute before:start-5 before:top-4 before:bottom-4 before:w-0.5 before:bg-border-subtle">
        {thread.replies.map((reply) => {
          const isOwnReply = reply.authorId === currentUserId;
          const replyVoteTitle = isOwnReply ? t("actions.voteOwnPost") : undefined;
          return (
          <div key={reply.id} className="relative ps-12">
            <div className="bg-bg-elevated p-6 rounded-2xl border border-border-subtle shadow-sm">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-2">
                  <UserAvatar
                    userId={reply.authorId}
                    name={reply.authorName}
                    className="w-7 h-7 border border-white shadow-sm"
                    initialsClassName="text-[10px]"
                  />
                  <span className="text-sm font-bold">{reply.authorName}</span>
                  <span className="text-xs text-text-tertiary">
                    {formatDistanceToNow(new Date(reply.createdAt), { addSuffix: true, locale: dateLocale })}
                  </span>
                </div>
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => handleVote(reply.id, "Up")}
                    disabled={isOwnReply}
                    title={replyVoteTitle}
                    aria-label="Upvote reply"
                    className="p-1 text-text-tertiary hover:text-brand-500 transition-colors disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:text-text-tertiary"
                  >
                    <ArrowUp size={16} />
                  </button>
                  <span className="text-xs font-bold w-4 text-center">{reply.upvoteCount - reply.downvoteCount}</span>
                  <button
                    type="button"
                    onClick={() => handleFlag(reply.id)}
                    className="p-1 text-text-tertiary hover:text-danger-500 transition-colors"
                  >
                    <Flag size={14} />
                  </button>
                </div>
              </div>
              <div className="text-text-secondary text-sm leading-relaxed">
                {reply.bodyMarkdown.split('\n').map((line, idx) => (
                  <p key={idx}>{line}</p>
                ))}
              </div>
            </div>
          </div>
          );
        })}
      </div>
    </div>
  );
}
