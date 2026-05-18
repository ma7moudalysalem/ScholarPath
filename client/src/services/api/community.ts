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
  title?: string;
  bodyMarkdown: string;
  upvoteCount: number;
  downvoteCount: number;
  replyCount: number;
  createdAt: string;
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
}

export const communityApi = {
  async getCategories(): Promise<ForumCategory[]> {
    const { data } = await apiClient.get<ForumCategory[]>("/api/community/categories");
    return data;
  },

  async getPosts(params: {
    categoryId?: string;
    query?: string;
    sortBy?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<ForumPost>> {
    const { data } = await apiClient.get<PagedResult<ForumPost>>("/api/community/posts", { params });
    return data;
  },

  async getPostDetails(id: string): Promise<ForumThread> {
    const { data } = await apiClient.get<ForumThread>(`/api/community/posts/${id}`);
    return data;
  },

  async createPost(req: { categoryId: string; title: string; bodyMarkdown: string }): Promise<string> {
    const { data } = await apiClient.post<string>("/api/community/posts", req);
    return data;
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
