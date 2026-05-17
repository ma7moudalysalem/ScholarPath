import { apiClient } from "@/services/api/client";
import type { FundingType, AcademicLevel, ListingMode } from "@/types/domain";

// ── DTOs ──────────────────────────────────────────────────────────────────────

export interface ScholarshipListItem {
  id: string;
  slug: string;
  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;
  deadline: string;
  fundingType: FundingType;
  mode: ListingMode;
  targetLevel: AcademicLevel;
  categoryId?: string | null;
  isFeatured: boolean;
  reviewFeeUsd?: number | null;
  ownerCompanyId?: string | null;
  country?: string | null;
  tags?: string[];
  externalUrl?: string | null;
  status: "Draft" | "Open" | "Closed" | "Archived" | "UnderReview";
  matchScore?: number | null;
}

export interface ScholarshipDetail extends ScholarshipListItem {
  formSchemaJson?: string | null;
  requiredDocuments?: string[];
  eligibilityCriteria?: string | null;
  ownerCompanyName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface SearchScholarshipsRequest {
  query?: string;
  countries?: string[];
  deadlineFrom?: string;
  deadlineTo?: string;
  fundingTypes?: FundingType[];
  academicLevels?: AcademicLevel[];
  tags?: string[];
  isFeatured?: boolean;
  sort?: "relevance" | "deadline" | "newest" | "recommended";
  page?: number;
  pageSize?: number;
}

export interface Paginated<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface BookmarkedScholarship {
  id: string;
  scholarshipId: string;
  scholarship: ScholarshipListItem;
  savedAt: string;
}

export type ScholarshipStatus =
  | "Draft"
  | "Open"
  | "Closed"
  | "Archived"
  | "UnderReview";

/** Server MyScholarshipDto shape — company list + admin moderation rows. */
export interface MyScholarship {
  id: string;
  titleEn: string;
  titleAr: string;
  slug: string | null;
  status: ScholarshipStatus;
  mode: ListingMode;
  deadline: string;
  applicantCount: number;
  createdAt: string;
}

/** Server PaginatedList<MyScholarshipDto> shape. */
export interface PaginatedMyScholarships {
  items: MyScholarship[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const scholarshipsApi = {
  async search(
    req: SearchScholarshipsRequest,
  ): Promise<Paginated<ScholarshipListItem>> {
    const { data } = await apiClient.post<Paginated<ScholarshipListItem>>(
      "/api/scholarships/search",
      req,
    );
    return data;
  },

  async getById(id: string): Promise<ScholarshipDetail> {
    const { data } = await apiClient.get<ScholarshipDetail>(
      `/api/scholarships/${id}`,
    );
    return data;
  },

  async toggleBookmark(id: string): Promise<{ bookmarked: boolean }> {
    const { data } = await apiClient.post<{ bookmarked: boolean }>(
      `/api/scholarships/${id}/bookmark`,
    );
    return data;
  },

  async getBookmarks(): Promise<BookmarkedScholarship[]> {
    const { data } = await apiClient.get<BookmarkedScholarship[]>(
      "/api/scholarships/bookmarks",
    );
    return data;
  },

  async getFeatured(): Promise<ScholarshipListItem[]> {
    const { data } = await apiClient.get<ScholarshipListItem[]>(
      "/api/scholarships/featured",
    );
    return data;
  },

  // ── Company: own scholarships ────────────────────────────────────────────────

  /** The authenticated company's own scholarships, newest first. */
  async getMine(): Promise<MyScholarship[]> {
    const { data } = await apiClient.get<MyScholarship[]>("/api/scholarships/mine");
    return data;
  },

  // ── Admin moderation ─────────────────────────────────────────────────────────

  /** Admin-only: scholarships filtered by moderation status, paged. */
  async getForModeration(
    status: ScholarshipStatus = "UnderReview",
    page = 1,
    pageSize = 20,
  ): Promise<PaginatedMyScholarships> {
    const { data } = await apiClient.get<PaginatedMyScholarships>(
      "/api/scholarships/admin",
      { params: { status, page, pageSize } },
    );
    return data;
  },

  /** Admin-only: approve an under-review scholarship. */
  async approve(id: string): Promise<void> {
    await apiClient.post(`/api/scholarships/${id}/approve`);
  },

  /** Admin-only: reject an under-review scholarship back to draft with a reason. */
  async reject(id: string, reason: string): Promise<void> {
    await apiClient.post(`/api/scholarships/${id}/reject`, { reason });
  },
};