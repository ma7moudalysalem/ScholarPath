import { apiClient } from "@/services/api/client";
import type { FundingType, AcademicLevel, ListingMode } from "@/types/domain";

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
}

export interface SearchScholarshipsRequest {
  query?: string;
  countries?: string[];
  deadlineFrom?: string;
  deadlineTo?: string;
  fundingTypes?: FundingType[];
  academicLevels?: AcademicLevel[];
  tags?: string[];
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

export const scholarshipsApi = {
  async search(req: SearchScholarshipsRequest): Promise<Paginated<ScholarshipListItem>> {
    const { data } = await apiClient.post<Paginated<ScholarshipListItem>>(
      "/api/scholarships/search",
      req,
    );
    return data;
  },

  async getById(id: string): Promise<ScholarshipListItem> {
    const { data } = await apiClient.get<ScholarshipListItem>(`/api/scholarships/${id}`);
    return data;
  },

  async toggleBookmark(id: string): Promise<{ bookmarked: boolean }> {
    const { data } = await apiClient.post<{ bookmarked: boolean }>(
      `/api/scholarships/${id}/bookmark`,
    );
    return data;
  },
};
