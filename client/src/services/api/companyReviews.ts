import { apiClient } from "@/services/api/client";

export interface CompanyReviewRow {
  reviewId: string;
  studentId: string;
  studentName: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface CompanyRatingsSummaryDto {
  companyId: string;
  averageRating: number;
  totalRatings: number;
  recentReviews: CompanyReviewRow[];
}

export const companyReviewsApi = {
  async getCompanyRatings(companyId: string, page = 1, pageSize = 25): Promise<CompanyRatingsSummaryDto> {
    const { data } = await apiClient.get<CompanyRatingsSummaryDto>(`/api/companies/${companyId}/reviews`, {
      params: { page, pageSize },
    });
    return data;
  },
};
