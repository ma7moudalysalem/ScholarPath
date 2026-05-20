import { apiClient } from "@/services/api/client";

// ─── enums (the API serialises enums as strings on the wire) ─────────────────

export type ResourceType = "Article" | "Guide" | "Checklist" | "VideoLink";

export type ResourceStatus =
  | "Draft"
  | "PendingReview"
  | "Published"
  | "Hidden"
  | "Removed";

// ─── DTOs ────────────────────────────────────────────────────────────────────

export interface ResourceListItem {
  id: string;
  slug: string;
  titleEn: string;
  titleAr: string;
  descriptionEn: string | null;
  descriptionAr: string | null;
  type: ResourceType;
  status: ResourceStatus;
  categorySlug: string | null;
  coverImageUrl: string | null;
  authorRole: string;
  tags: string[];
  isFeatured: boolean;
  featuredOrder: number;
  publishedAt: string | null;
}

/** Server PaginatedList<T> shape on the wire. */
export interface PaginatedResources {
  items: ResourceListItem[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** One chapter of a multi-part resource. */
export interface ResourceChapter {
  id: string;
  titleEn: string;
  titleAr: string;
  contentMarkdownEn: string | null;
  contentMarkdownAr: string | null;
  sortOrder: number;
  estimatedReadMinutes: number;
}

/** Full resource detail — server ResourceDetailDto. */
export interface ResourceDetail {
  id: string;
  slug: string;
  titleEn: string;
  titleAr: string;
  descriptionEn: string | null;
  descriptionAr: string | null;
  contentMarkdownEn: string | null;
  contentMarkdownAr: string | null;
  externalLinkUrl: string | null;
  coverImageUrl: string | null;
  authorUserId: string;
  authorRole: string;
  authorName: string | null;
  type: ResourceType;
  status: ResourceStatus;
  categorySlug: string | null;
  tags: string[];
  isFeatured: boolean;
  publishedAt: string | null;
  rejectionReason: string | null;
  chapters: ResourceChapter[];
}

export interface SearchResourcesParams {
  term?: string;
  categorySlug?: string;
  tag?: string;
  authorRole?: string;
  type?: ResourceType;
  language?: string;
  page?: number;
  pageSize?: number;
}

export interface ResourceChapterInput {
  titleEn: string;
  titleAr: string;
  contentMarkdownEn?: string;
  contentMarkdownAr?: string;
  sortOrder: number;
  estimatedReadMinutes: number;
}

export interface CreateResourceInput {
  titleEn: string;
  titleAr: string;
  descriptionEn?: string;
  descriptionAr?: string;
  contentMarkdownEn?: string;
  contentMarkdownAr?: string;
  externalLinkUrl?: string;
  coverImageUrl?: string;
  type: ResourceType;
  categorySlug?: string;
  tags?: string[];
  chapters?: ResourceChapterInput[];
}

export interface UpdateResourceInput extends CreateResourceInput {
  /* same shape — ResourceId is passed as path param */
}

export interface ResourceProgressItem {
  resourceId: string;
  slug: string;
  titleEn: string;
  titleAr: string;
  chaptersCompletedCount: number;
  totalChapters: number;
  lastAccessedAt: string;
}

export interface ChapterProgressResult {
  resourceId: string;
  chapterId: string;
  totalChapters: number;
  chaptersCompleted: number;
  isResourceComplete: boolean;
}

// ─── API ─────────────────────────────────────────────────────────────────────

export const resourcesApi = {
  /** Public browse/search over published resources. */
  async search(params: SearchResourcesParams = {}): Promise<PaginatedResources> {
    const { data } = await apiClient.get<PaginatedResources>("/api/resources", {
      params,
    });
    return data;
  },

  /** Featured resources for the hub. */
  async getFeatured(): Promise<ResourceListItem[]> {
    const { data } = await apiClient.get<ResourceListItem[]>("/api/resources/featured");
    return data;
  },

  /** Full detail of a single resource, by id or slug. */
  async getDetail(idOrSlug: string): Promise<ResourceDetail> {
    const { data } = await apiClient.get<ResourceDetail>(
      `/api/resources/${encodeURIComponent(idOrSlug)}`,
    );
    return data;
  },

  // ── Author CRUD ────────────────────────────────────────────────────────────

  /** Create a new resource (Consultant / Company / Admin only). Returns the new id. */
  async create(input: CreateResourceInput): Promise<string> {
    const { data } = await apiClient.post<string>("/api/resources", input);
    return data;
  },

  /** Update an existing draft resource. */
  async update(id: string, input: UpdateResourceInput): Promise<void> {
    await apiClient.put(`/api/resources/${id}`, input);
  },

  /** Submit a draft for review (or publish directly if the caller is an admin). */
  async submit(id: string): Promise<ResourceStatus> {
    const { data } = await apiClient.post<ResourceStatus>(`/api/resources/${id}/submit`);
    return data;
  },

  /** The caller's own resources, any status. */
  async getMine(): Promise<ResourceListItem[]> {
    const { data } = await apiClient.get<ResourceListItem[]>("/api/resources/mine");
    return data;
  },

  // ── Student progress & bookmarks ───────────────────────────────────────────

  /** Toggle bookmark for a resource. Returns true if now bookmarked. */
  async toggleBookmark(id: string): Promise<boolean> {
    const { data } = await apiClient.post<boolean>(`/api/resources/${id}/bookmark`);
    return data;
  },

  /** The caller's bookmarked resources. */
  async getMyBookmarks(): Promise<ResourceListItem[]> {
    const { data } = await apiClient.get<ResourceListItem[]>("/api/resources/bookmarks/me");
    return data;
  },

  /** Mark a chapter as completed. */
  async completeChapter(resourceId: string, chapterId: string): Promise<ChapterProgressResult> {
    const { data } = await apiClient.post<ChapterProgressResult>(
      `/api/resources/${resourceId}/chapters/${chapterId}/complete`,
    );
    return data;
  },

  /** The caller's reading progress across all resources. */
  async getMyProgress(): Promise<ResourceProgressItem[]> {
    const { data } = await apiClient.get<ResourceProgressItem[]>("/api/resources/progress/me");
    return data;
  },

  // ── Admin moderation ───────────────────────────────────────────────────────

  /** Admin-only: resources awaiting review. */
  async getPendingReview(): Promise<ResourceListItem[]> {
    const { data } = await apiClient.get<ResourceListItem[]>(
      "/api/resources/pending-review",
    );
    return data;
  },

  /** Admin-only: approve a pending resource. */
  async approve(id: string): Promise<void> {
    await apiClient.post(`/api/resources/${id}/approve`);
  },

  /** Admin-only: reject a pending resource back to draft with a reason. */
  async reject(id: string, rejectionReason: string): Promise<void> {
    await apiClient.post(`/api/resources/${id}/reject`, { rejectionReason });
  },

  /** Admin-only: toggle featured status. */
  async setFeatured(id: string, featured: boolean): Promise<void> {
    await apiClient.post(`/api/resources/${id}/feature`, { featured });
  },

  /** Admin-only: set visibility (Hidden / Published / Removed). */
  async setVisibility(id: string, status: ResourceStatus): Promise<void> {
    await apiClient.post(`/api/resources/${id}/visibility`, { status });
  },
};
