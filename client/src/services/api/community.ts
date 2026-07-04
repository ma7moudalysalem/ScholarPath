import { apiClient } from "@/services/api/client";
import type { PagedResult } from "@/types/api";

export interface ForumCategory {
  id: string;
  nameEn: string;
  nameAr: string;
  slug: string;
  descriptionEn?: string;
  descriptionAr?: string;
  displayOrder: number;
}

export interface ForumPost {
  id: string;
  authorId: string;
  authorName: string;
  categoryId?: string;
  /** Legacy single-language view (English side). Prefer the bilingual pair. */
  title?: string;
  bodyMarkdown: string;
  /** Bilingual content — display with a cross-language fallback (see helpers). */
  titleEn?: string | null;
  titleAr?: string | null;
  bodyEn?: string;
  bodyAr?: string | null;
  upvoteCount: number;
  downvoteCount: number;
  replyCount: number;
  createdAt: string;
  tags: string[];
  isBookmarked: boolean;
}

/** Localized post title — current language first, then the other, then legacy. */
export function forumPostTitle(p: ForumPost, isRtl: boolean): string {
  return (
    (isRtl ? p.titleAr || p.titleEn : p.titleEn || p.titleAr) ??
    p.title ??
    ""
  );
}

/** Localized post body — current language first, then the other, then legacy. */
export function forumPostBody(p: ForumPost, isRtl: boolean): string {
  return (
    (isRtl ? p.bodyAr || p.bodyEn : p.bodyEn || p.bodyAr) ||
    p.bodyMarkdown
  );
}

export interface ForumThread {
  post: ForumPost;
  replies: ForumPost[];
}

/** Vote direction on the wire — must match the server VoteType enum (Up / Down). */
export type VoteType = "Up" | "Down";

export type PostModerationStatus =
  | "Visible"
  | "Hidden"
  | "Removed"
  | "PendingReview";

/** Server PagedResult<T> record shape on the wire. */
export interface CommunityPagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** An individual report on a flagged post (shown to the admin). */
export interface FlagDetail {
  reason: string;
  additionalDetails: string | null;
  flaggedAt: string;
}

/** Server FlaggedPostDto shape — a post in the admin moderation queue. */
export interface FlaggedPost {
  id: string;
  authorId: string;
  authorName: string;
  title: string | null;
  bodyPreview: string;
  flagCount: number;
  validFlagCount: number;
  topFlagReason: string | null;
  moderationStatus: PostModerationStatus;
  isAutoHidden: boolean;
  autoHiddenAt: string | null;
  createdAt: string;
  flags: FlagDetail[];
}

export interface GetPostsParams {
  categoryId?: string;
  query?: string;
  sortBy?: string;
  tag?: string;
  page?: number;
  pageSize?: number;
}

export const communityApi = {
  async getCategories(): Promise<ForumCategory[]> {
    const { data } = await apiClient.get<ForumCategory[]>("/api/community/categories");
    return data;
  },

  async getPosts(params: GetPostsParams): Promise<PagedResult<ForumPost>> {
    const { data } = await apiClient.get<PagedResult<ForumPost>>("/api/community/posts", { params });
    return data;
  },

  async getPostDetails(id: string): Promise<ForumThread> {
    const { data } = await apiClient.get<ForumThread>(`/api/community/posts/${id}`);
    return data;
  },

  async createPost(req: {
    categoryId: string;
    titleEn: string;
    titleAr: string;
    bodyEn: string;
    bodyAr: string;
    tags?: string[];
  }): Promise<string> {
    const { data } = await apiClient.post<string>("/api/community/posts", req);
    return data;
  },

  async updatePost(
    postId: string,
    req: {
      titleEn?: string | null;
      titleAr?: string | null;
      bodyEn: string;
      bodyAr?: string | null;
      tags?: string[];
    },
  ): Promise<void> {
    await apiClient.put(`/api/community/posts/${postId}`, req);
  },

  async deletePost(postId: string): Promise<void> {
    await apiClient.delete(`/api/community/posts/${postId}`);
  },

  async createReply(postId: string, req: { bodyMarkdown: string }): Promise<string> {
    const { data } = await apiClient.post<string>(`/api/community/posts/${postId}/replies`, req);
    return data;
  },

  async toggleVote(postId: string, voteType: VoteType): Promise<void> {
    await apiClient.post(`/api/community/posts/${postId}/vote`, { voteType });
  },

  async flagPost(postId: string, req: { reason: string; additionalDetails?: string }): Promise<void> {
    await apiClient.post(`/api/community/posts/${postId}/flag`, req);
  },

  // ── Bookmarks ──────────────────────────────────────────────────────────────

  /**
   * Toggles a bookmark on a root post. Returns the new state — true when
   * the post is now saved, false when it was removed.
   */
  async toggleBookmark(postId: string): Promise<boolean> {
    const { data } = await apiClient.post<{ bookmarked: boolean }>(
      `/api/community/posts/${postId}/bookmark`,
    );
    return data.bookmarked;
  },

  /** Lists the current student's bookmarked posts. Student-only. */
  async getMyBookmarks(
    page = 1,
    pageSize = 20,
  ): Promise<PagedResult<ForumPost>> {
    const { data } = await apiClient.get<PagedResult<ForumPost>>(
      "/api/community/bookmarks",
      { params: { page, pageSize } },
    );
    return data;
  },

  // ── Admin moderation ───────────────────────────────────────────────────────

  /** Admin-only: posts in the moderation queue — flagged or auto-hidden. */
  async getFlaggedPosts(
    page = 1,
    pageSize = 20,
  ): Promise<CommunityPagedResult<FlaggedPost>> {
    const { data } = await apiClient.get<CommunityPagedResult<FlaggedPost>>(
      "/api/community/admin/flagged",
      { params: { page, pageSize } },
    );
    return data;
  },

  /** Admin-only: remove a flagged post (soft-delete). */
  async removePost(postId: string): Promise<void> {
    await apiClient.post(`/api/community/admin/posts/${postId}/remove`);
  },

  /** Admin-only: dismiss the flags on a post ("keep"). */
  async dismissFlags(postId: string): Promise<void> {
    await apiClient.post(`/api/community/admin/posts/${postId}/dismiss`);
  },
};
