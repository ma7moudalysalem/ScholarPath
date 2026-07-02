import { apiClient } from "@/services/api/client";

export interface ScholarshipProviderReviewRow {
  reviewId: string;
  studentId: string;
  studentName: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface ScholarshipProviderRatingsSummaryDto {
  scholarshipProviderId: string;
  averageRating: number;
  totalRatings: number;
  recentReviews: ScholarshipProviderReviewRow[];
}

export const scholarshipProviderReviewsApi = {
  async getScholarshipProviderRatings(scholarshipProviderId: string, page = 1, pageSize = 25): Promise<ScholarshipProviderRatingsSummaryDto> {
    const { data } = await apiClient.get<ScholarshipProviderRatingsSummaryDto>(`/api/companies/${scholarshipProviderId}/reviews`, {
      params: { page, pageSize },
    });
    return data;
  },
};
