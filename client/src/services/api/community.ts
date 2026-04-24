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

  async toggleVote(postId: string, voteType: "Upvote" | "Downvote"): Promise<void> {
    await apiClient.post(`/api/community/posts/${postId}/vote`, { voteType });
  },

  async flagPost(postId: string, req: { reason: string; additionalDetails?: string }): Promise<void> {
    await apiClient.post(`/api/community/posts/${postId}/flag`, req);
  },
};
