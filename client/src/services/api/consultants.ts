import { apiClient } from "@/services/api/client";

// ── View models (what the marketplace pages consume) ──────────────────────────
//
// The consultant DTOs are flat camelCase, so these view models mirror the wire
// shape 1:1 — the service exists as the single typed seam between the API and
// the pages, mirroring `scholarships.ts`.

/** One consultant card in the public browse list — `GET /api/consultants`. */
export interface ConsultantSummary {
  id: string;
  name: string;
  photoUrl?: string | null;
  biography?: string | null;
  expertiseTags: string[];
  languages: string[];
  sessionFeeUsd?: number | null;
  sessionDurationMinutes?: number | null;
  averageRating?: number | null;
  reviewCount: number;
  completedSessionCount: number;
  activeAvailabilityRuleCount: number;
  hasAvailability: boolean;
}

/** One public review row on a consultant's detail page. */
export interface ConsultantReview {
  id: string;
  rating: number;
  comment?: string | null;
  studentName: string;
  createdAt: string;
}

/** Full consultant profile detail — `GET /api/consultants/{id}`. */
export interface ConsultantDetail {
  id: string;
  name: string;
  photoUrl?: string | null;
  countryOfResidence?: string | null;
  biography?: string | null;
  linkedInUrl?: string | null;
  websiteUrl?: string | null;
  timezone?: string | null;
  expertiseTags: string[];
  languages: string[];
  sessionFeeUsd?: number | null;
  sessionDurationMinutes?: number | null;
  averageRating?: number | null;
  reviewCount: number;
  completedSessionCount: number;
  hasAvailability: boolean;
  recentReviews: ConsultantReview[];
}

/**
 * A concrete, bookable time window for a consultant —
 * `GET /api/consultants/{id}/availability` (next 28 days).
 */
export interface BookableSlot {
  availabilityId: string;
  startAt: string;
  endAt: string;
  durationMinutes: number;
  isRecurring: boolean;
  timezone: string;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const consultantsApi = {
  /** Lists every active consultant with a profile summary. Anonymous-accessible. */
  async browse(): Promise<ConsultantSummary[]> {
    const { data } = await apiClient.get<ConsultantSummary[]>("/api/consultants");
    return data;
  },

  /** Returns one consultant's full public profile. Anonymous-accessible. */
  async getById(id: string): Promise<ConsultantDetail> {
    const { data } = await apiClient.get<ConsultantDetail>(`/api/consultants/${id}`);
    return data;
  },

  /** Returns a consultant's upcoming bookable slots (next 28 days). */
  async getAvailability(id: string): Promise<BookableSlot[]> {
    const { data } = await apiClient.get<BookableSlot[]>(
      `/api/consultants/${id}/availability`,
    );
    return data;
  },
};
