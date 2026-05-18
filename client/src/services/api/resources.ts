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
};
