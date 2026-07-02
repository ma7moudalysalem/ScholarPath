import { apiClient } from "@/services/api/client";

// ── Reviews-received view models (PB "Reviews received" page) ─────────────────
//
// The company and consultant "reviews received" surfaces share one wire shape:
// the server returns the same `ReceivedReviewsSummaryDto` from both
// `GET /api/company-reviews/mine` and `GET /api/consultant/reviews/mine`, so a
// single typed model backs the shared page component.

/** One received review as shown to the rated party. Author name is masked server-side. */
export interface ReceivedReview {
  id: string;
  rating: number;
  comment?: string | null;
  authorName: string;
  createdAt: string;
}

/** The authenticated user's received-reviews summary: aggregate + newest-first list. */
export interface ReceivedReviewsSummary {
  averageRating: number;
  totalReviews: number;
  reviews: ReceivedReview[];
}

export const reviewsApi = {
  /** Reviews received by the authenticated company. Requires the ScholarshipProvider role. */
  async scholarshipProviderMine(): Promise<ReceivedReviewsSummary> {
    const { data } = await apiClient.get<ReceivedReviewsSummary>(
      "/api/company-reviews/mine",
    );
    return data;
  },

  /** Reviews received by the authenticated consultant. Requires the Consultant role. */
  async consultantMine(): Promise<ReceivedReviewsSummary> {
    const { data } = await apiClient.get<ReceivedReviewsSummary>(
      "/api/consultant/reviews/mine",
    );
    return data;
  },
};
