import { apiClient } from "@/services/api/client";

/** Wire shape for `POST /api/upgrade-requests/consultant`. */
export interface SubmitConsultantUpgradeRequestPayload {
  biography: string;
  professionalTitle: string;
  highestDegree: string;
  fieldOfExpertise: string;
  yearsOfExperience: number | null;
  sessionFeeUsd: number | null;
  sessionDurationMinutes: number | null;
  expertiseTags: string[] | null;
  languages: string[] | null;
  country: string;
  timezone: string;
  linkedInUrl?: string | null;
  portfolioUrl?: string | null;
}

export interface SubmitUpgradeResponse {
  requestId: string;
}

export const upgradeRequestsApi = {
  /**
   * Submit a Student → Consultant upgrade request. The backing endpoint
   * persists the consultant profile fields and creates a Pending UpgradeRequest
   * row that the admin upgrade queue picks up.
   */
  async submitConsultantUpgradeRequest(
    payload: SubmitConsultantUpgradeRequestPayload,
  ): Promise<SubmitUpgradeResponse> {
    const { data } = await apiClient.post<SubmitUpgradeResponse>(
      "/api/upgrade-requests/consultant",
      payload,
    );
    return data;
  },
};
